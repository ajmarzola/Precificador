# Precificador Frontend

Cliente Next.js para todos os endpoints CRUD da `Precificador.WebApi`.

## Executar

1. Inicie a API: `dotnet run --project src/Precificador.WebApi --launch-profile http`
2. Copie `.env.example` para `.env.local` se a API estiver em outro endereço.
3. No diretório `frontend`, execute `npm install` e `npm run dev`.

O cliente usa `http://localhost:8080` por padrão. As consultas `ById` e `ByFilter` passam por uma rota proxy do Next.js para manter compatibilidade com a API atual, que recebe o corpo em requisições GET.
