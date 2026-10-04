function Invoke-Az {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = & az @Arguments --only-show-errors 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($exitCode -ne 0) {
        throw "Azure CLI falhou na operacao $($Arguments[0]) (exit $exitCode)."
    }

    return $output
}

function Invoke-AzJson {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $output = Invoke-Az ($Arguments + @("-o", "json"))
    $text = $output -join [Environment]::NewLine
    if ([string]::IsNullOrWhiteSpace($text)) {
        return $null
    }

    return $text | ConvertFrom-Json
}

function Invoke-AzJsonOrNull {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = & az @Arguments --only-show-errors -o json 2>&1
        $exitCode = $LASTEXITCODE
    } finally { $ErrorActionPreference = $previousErrorActionPreference }
    $text = $output -join [Environment]::NewLine
    if ($exitCode -ne 0) {
        # Apenas o codigo explicito do recurso ausente autoriza criacao.
        # ResourceGroupNotFound, autorizacao, rede e outros erros falham fechado.
        if ($text -match '(?m)^\s*ERROR:\s*\(ResourceNotFound\)') { return $null }
        throw "Leitura Azure falhou na operacao $($Arguments[0]) (exit $exitCode)."
    }
    if ([string]::IsNullOrWhiteSpace($text)) { throw "Leitura Azure retornou resposta vazia." }
    $result = $text | ConvertFrom-Json -NoEnumerate -ErrorAction Stop
    if ($null -eq $result) { throw "Leitura Azure retornou null sem confirmar ausencia." }
    if ($result -is [array] -or $result -isnot [pscustomobject]) { throw "Leitura Azure nao retornou objeto unico." }
    return $result
}

function Get-CdIdentityOrNull {
    param([string]$IdentityName)
    $identity = Invoke-AzJsonOrNull @("identity", "show", "-g", $ResourceGroupName, "-n", $IdentityName)
    if ($null -eq $identity) { return $null }
    if ($identity -isnot [pscustomobject]) { throw "Resposta de identidade Azure invalida." }
    foreach ($property in @("id", "name", "tenantId", "clientId", "principalId")) {
        if ($null -eq $identity.PSObject.Properties[$property] -or
            [string]::IsNullOrWhiteSpace([string]$identity.PSObject.Properties[$property].Value)) {
            throw "Resposta de identidade Azure incompleta."
        }
    }
    $expectedId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.ManagedIdentity/userAssignedIdentities/$IdentityName"
    if ($identity.name -ne $IdentityName -or $identity.id -ne $expectedId -or $identity.tenantId -ne $TenantId) {
        throw "Identidade Azure retornada diverge do destino esperado."
    }
    foreach ($property in @("tenantId", "clientId", "principalId")) {
        $parsed = [guid]::Empty
        if (-not [guid]::TryParse([string]$identity.$property, [ref]$parsed) -or $parsed -eq [guid]::Empty) {
            throw "Identificador de identidade Azure invalido."
        }
    }
    return $identity
}

function Get-AzTsv {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $output = Invoke-Az ($Arguments + @("-o", "tsv"))
    return (($output -join [Environment]::NewLine).Trim())
}

function Assert-AzLogin {
    $account = Invoke-AzJson @("account", "show", "--query", "{id:id,name:name,state:state,tenantId:tenantId}")
    if ($null -eq $account -or $account.state -ne "Enabled") {
        throw "A assinatura Azure selecionada precisa estar ativa/Enabled antes do deploy."
    }

    if ($account.tenantId -ne $TenantId) {
        throw "Tenant Azure selecionado diverge do TenantId informado. Selecione manualmente o contexto correto antes do deploy."
    }

    if ($account.id -ne $SubscriptionId) {
        throw "Assinatura Azure selecionada diverge do SubscriptionId informado. Selecione manualmente o contexto correto antes do deploy."
    }

    Write-Host "Assinatura validada: $($account.name) ($($account.id)); tenant $($account.tenantId)."
}

function Get-PublicIp {
    param([string]$ExplicitIp)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitIp)) {
        return $ExplicitIp
    }

    try {
        return (Invoke-RestMethod -Uri "https://api.ipify.org" -TimeoutSec 20).Trim()
    }
    catch {
        throw "Nao foi possivel detectar o IP publico do operador. Informe -OperatorIpAddress para migrations."
    }
}

function Add-SqlFirewallIp {
    param(
        [string]$RuleName,
        [string]$IpAddress
    )

    $parsedIp = $null
    if (-not [System.Net.IPAddress]::TryParse($IpAddress, [ref]$parsedIp) -or
        $parsedIp.AddressFamily -ne [System.Net.Sockets.AddressFamily]::InterNetwork -or $IpAddress -eq "0.0.0.0") {
        throw "IP publico IPv4 invalido; firewall nao sera aberto."
    }
    Invoke-Az @(
        "sql", "server", "firewall-rule", "create",
        "-g", $ResourceGroupName,
        "-s", $SqlServerName,
        "-n", $RuleName,
        "--start-ip-address", $IpAddress,
        "--end-ip-address", $IpAddress
    ) | Out-Null
}

function Remove-SqlFirewallRuleIfExists {
    param([string]$RuleName)
    # Uma falha de leitura nunca equivale a regra ausente.
    $rules = @(Invoke-AzJson @("sql", "server", "firewall-rule", "list", "-g", $ResourceGroupName, "-s", $SqlServerName))
    if ($rules | Where-Object { $_.name -eq $RuleName }) {
        Invoke-Az @("sql", "server", "firewall-rule", "delete", "-g", $ResourceGroupName, "-s", $SqlServerName, "-n", $RuleName) | Out-Null
    }
    $remaining = @(Invoke-AzJson @("sql", "server", "firewall-rule", "list", "-g", $ResourceGroupName, "-s", $SqlServerName))
    if ($remaining | Where-Object { $_.name -eq $RuleName }) { throw "Firewall temporario nao foi removido." }
}

function Assert-FreeResources {
    $planSku = Get-AzTsv @("appservice", "plan", "show", "-g", $ResourceGroupName, "-n", $AppServicePlanName, "--query", "sku.name")
    if ($planSku -ne "F1") {
        throw "Deploy interrompido: App Service Plan nao esta em F1. Valor encontrado: $planSku."
    }

    $db = Invoke-AzJson @("sql", "db", "show", "-g", $ResourceGroupName, "-s", $SqlServerName, "-n", $SqlDatabaseName)
    $dbText = $db | ConvertTo-Json -Depth 100
    if ($dbText -notmatch '"useFreeLimit"\s*:\s*true') {
        throw "Deploy interrompido: Azure SQL nao confirmou useFreeLimit=true."
    }
    if ($dbText -notmatch '"freeLimitExhaustionBehavior"\s*:\s*"AutoPause"') {
        throw "Deploy interrompido: Azure SQL nao confirmou freeLimitExhaustionBehavior=AutoPause."
    }
}

function Invoke-MigrationWithFirewall {
    param(
        [string]$RuleName,
        [string]$IpAddress,
        [string]$ConnectionString,
        [scriptblock]$Migration
    )
    $previousConnection = $env:ConnectionStrings__Precificador
    try {
        Add-SqlFirewallIp -RuleName $RuleName -IpAddress $IpAddress
        $env:ConnectionStrings__Precificador = $ConnectionString
        & $Migration
    } finally {
        $env:ConnectionStrings__Precificador = $previousConnection
        Remove-SqlFirewallRuleIfExists -RuleName $RuleName
    }
}


function Assert-DeploymentTarget {
    Assert-AzLogin
    Assert-FreeResources
    $webApp = Invoke-AzJson @("webapp", "show", "-g", $ResourceGroupName, "-n", $AppName)
    if ($webApp.httpsOnly -ne $true) {
        throw "Deploy interrompido: HTTPS Only nao esta habilitado na Web App."
    }

    $settings = Invoke-AzJson @("webapp", "config", "appsettings", "list", "-g", $ResourceGroupName, "-n", $AppName)
    $connectionSetting = $settings | Where-Object { $_.name -eq "ConnectionStrings__Precificador" } | Select-Object -First 1
    if ($null -eq $connectionSetting) { throw "ConnectionStrings__Precificador ausente." }
    $planId = Get-AzTsv @("appservice", "plan", "show", "-g", $ResourceGroupName, "-n", $AppServicePlanName, "--query", "id")
    if ($webApp.serverFarmId -ne $planId) { throw "Web App nao pertence ao plano validado." }
    $builder = New-Object System.Data.Common.DbConnectionStringBuilder
    $builder.set_ConnectionString($connectionSetting.value)
    if (-not $builder.ContainsKey("Authentication") -or
        $builder.get_Item("Authentication") -ne "Active Directory Managed Identity") {
        throw "Conexao runtime nao confirma Managed Identity passwordless."
    }
    foreach ($key in @("Password", "PWD", "User ID", "UID")) {
        if ($builder.ContainsKey($key)) { throw "Conexao runtime contem credencial." }
    }
    $timeout = 0
    if (-not $builder.ContainsKey("Connection Timeout") -or
        -not [int]::TryParse([string]$builder.get_Item("Connection Timeout"), [ref]$timeout) -or $timeout -lt 60) {
        throw "Conexao runtime exige Connection Timeout de pelo menos 60 segundos."
    }
    $server = if ($builder.ContainsKey("Server")) { $builder.get_Item("Server") } else { $builder.get_Item("Data Source") }
    $database = if ($builder.ContainsKey("Database")) { $builder.get_Item("Database") } else { $builder.get_Item("Initial Catalog") }
    if ($server -ne "tcp:$SqlServerName.database.windows.net,1433" -or $database -ne $SqlDatabaseName) {
        throw "Conexao runtime diverge do destino da migration."
    }
    if ([string]::IsNullOrWhiteSpace($webApp.identity.principalId)) { throw "Identidade runtime ausente." }
    return $webApp
}
