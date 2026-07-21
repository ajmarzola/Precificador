"use client";

import { FormEvent, useMemo, useState } from "react";

type Field = {
  key: string;
  label: string;
  type: "text" | "number" | "date" | "datetime-local" | "uuid";
  required?: boolean;
  min?: number;
  step?: string;
};

type Resource = {
  label: string;
  endpoint: string;
  fields: Field[];
  filterFields: Field[];
};

const uuid: Field = { key: "id", label: "ID", type: "uuid", required: true };
const nome: Field = { key: "nome", label: "Nome", type: "text", required: true };
const decimal = (key: string, label: string, required = true): Field => ({ key, label, type: "number", required, min: 0.01, step: "0.01" });

const resources: Resource[] = [
  { label: "Coleções", endpoint: "Colecao", fields: [nome, { key: "ano", label: "Ano", type: "number", required: true, min: 2016 }, { key: "dataLancamento", label: "Data de lançamento", type: "datetime-local" }], filterFields: [{ key: "nome", label: "Nome", type: "text" }, { key: "ano", label: "Ano", type: "number" }] },
  { label: "Produtos", endpoint: "Produto", fields: [nome, { key: "colecaoId", label: "ID da coleção", type: "uuid", required: true }, decimal("margem", "Margem"), { key: "dataCalculoPreco", label: "Data do cálculo", type: "datetime-local" }, decimal("precoCusto", "Preço de custo", false), decimal("precoFinal", "Preço final", false), decimal("precoCustoX3", "Custo x3", false), decimal("precoCustoX35", "Custo x3,5", false), decimal("precoCustoX4", "Custo x4", false)], filterFields: [{ key: "nome", label: "Nome", type: "text" }] },
  { label: "Matérias-primas", endpoint: "MateriaPrima", fields: [nome, decimal("qtdPacote", "Quantidade do pacote"), decimal("vlrPacote", "Valor do pacote"), { key: "dataPreco", label: "Data do preço", type: "datetime-local" }, decimal("vlrUnitario", "Valor unitário", false), { key: "unidadeMedidaId", label: "ID da unidade", type: "uuid", required: true }, { key: "grupoId", label: "ID do grupo", type: "uuid", required: true }], filterFields: [{ key: "nome", label: "Nome", type: "text" }] },
  { label: "Unidades de medida", endpoint: "UnidadeMedida", fields: [nome, { key: "abreviacao", label: "Abreviação", type: "text", required: true }], filterFields: [{ key: "nome", label: "Nome", type: "text" }] },
  { label: "Grupos", endpoint: "Grupo", fields: [nome], filterFields: [{ key: "nome", label: "Nome", type: "text" }] },
  { label: "Pesquisas de preço", endpoint: "PesquisaPreco", fields: [{ key: "produtoId", label: "ID do produto", type: "uuid", required: true }, { key: "local", label: "Local", type: "text", required: true }, decimal("valor", "Valor"), { key: "dataPesquisa", label: "Data da pesquisa", type: "datetime-local", required: true }], filterFields: [{ key: "produtoId", label: "ID do produto", type: "uuid" }, { key: "produtoNome", label: "Nome do produto", type: "text" }, { key: "local", label: "Local", type: "text" }, { key: "dataPesquisa", label: "Data", type: "date" }] },
  { label: "Composição do produto", endpoint: "ProdutoMateriaPrima", fields: [{ key: "produtoId", label: "ID do produto", type: "uuid", required: true }, { key: "materiaPrimaId", label: "ID da matéria-prima", type: "uuid", required: true }, decimal("quantidade", "Quantidade utilizada")], filterFields: [{ key: "produtoId", label: "ID do produto", type: "uuid" }, { key: "produtoNome", label: "Nome do produto", type: "text" }, { key: "materiaPrimaId", label: "ID da matéria-prima", type: "uuid" }, { key: "materiaPrimaNome", label: "Nome da matéria-prima", type: "text" }] },
  { label: "Produtos da coleção", endpoint: "ColecaoProduto", fields: [{ key: "colecaoId", label: "ID da coleção", type: "uuid", required: true }, { key: "produtoId", label: "ID do produto", type: "uuid", required: true }], filterFields: [{ key: "colecaoId", label: "ID da coleção", type: "uuid" }, { key: "colecaoNome", label: "Nome da coleção", type: "text" }, { key: "produtoId", label: "ID do produto", type: "uuid" }, { key: "produtoNome", label: "Nome do produto", type: "text" }] }
];

function initialValues(fields: Field[]) { return Object.fromEntries(fields.map((field) => [field.key, ""])); }

function normalize(values: Record<string, string>, fields: Field[]) {
  return Object.fromEntries(fields.filter((field) => values[field.key] !== "").map((field) => {
    const value = values[field.key];
    if (field.type === "number") return [field.key, Number(value)];
    if (field.type === "datetime-local") return [field.key, new Date(value).toISOString()];
    if (field.type === "date") return [field.key, `${value}T00:00:00`];
    return [field.key, value];
  }));
}

async function callApi(method: string, path: string, body?: unknown) {
  const serialized = body === undefined ? undefined : JSON.stringify(body);
  const url = method === "GET" && serialized ? `/api/proxy/${path}?body=${encodeURIComponent(serialized)}` : `/api/proxy/${path}`;
  const response = await fetch(url, { method, headers: serialized && method !== "GET" ? { "content-type": "application/json" } : undefined, body: serialized && method !== "GET" ? serialized : undefined });
  const text = await response.text();
  let data: unknown = null;
  try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  if (!response.ok) throw new Error(typeof data === "string" ? data : `Erro HTTP ${response.status}`);
  return { status: response.status, data };
}

export default function Home() {
  const [resourceIndex, setResourceIndex] = useState(0);
  const resource = resources[resourceIndex];
  const [values, setValues] = useState<Record<string, string>>({});
  const [filters, setFilters] = useState<Record<string, string>>({});
  const [id, setId] = useState("");
  const [result, setResult] = useState<unknown>(null);
  const [status, setStatus] = useState("Pronto para consultar a API.");
  const [loading, setLoading] = useState(false);

  const resultRows = useMemo(() => Array.isArray(result) ? result : result ? [result] : [], [result]);
  const columns = useMemo(() => [...new Set(resultRows.flatMap((row) => typeof row === "object" && row ? Object.keys(row as object) : []))], [resultRows]);

  function change(setter: typeof setValues, key: string, value: string) { setter((current) => ({ ...current, [key]: value })); }
  async function execute(label: string, action: () => Promise<{ status: number; data: unknown }>) {
    setLoading(true); setStatus(`${label} em andamento...`);
    try { const response = await action(); setResult(response.data); setStatus(`${label} concluído: HTTP ${response.status}.`); }
    catch (error) { setResult(null); setStatus(error instanceof Error ? error.message : "Ocorreu um erro inesperado."); }
    finally { setLoading(false); }
  }
  function submit(method: "POST" | "PUT") {
    return (event: FormEvent) => { event.preventDefault(); void execute(method === "POST" ? "Cadastro" : "Atualização", () => callApi(method, resource.endpoint, normalize(values, method === "PUT" ? [uuid, ...resource.fields] : resource.fields))); };
  }
  function selectResource(index: number) { setResourceIndex(index); setValues({}); setFilters({}); setId(""); setResult(null); setStatus("Recurso selecionado. Preencha os dados e execute uma operação."); }

  return <main>
    <aside>
      <div className="brand"><span>R$</span><div><strong>Precificador</strong><small>WebAPI Console</small></div></div>
      <nav aria-label="Recursos da API">{resources.map((item, index) => <button className={index === resourceIndex ? "active" : ""} key={item.endpoint} onClick={() => selectResource(index)}>{item.label}</button>)}</nav>
      <p className="api-note">API configurada em<br /><code>{process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080"}</code></p>
    </aside>
    <section className="content">
      <header><p>Cadastros e precificação</p><h1>{resource.label}</h1><span>Endpoint: <code>/api/{resource.endpoint}</code></span></header>
      <div className="status" role="status" data-error={status.startsWith("Erro") || status.startsWith("Não foi possível")}>{loading ? "Processando" : status}</div>
      <div className="operations">
        <article className="card list-card"><div className="card-title"><h2>Consultar registros</h2><button disabled={loading} onClick={() => void execute("Consulta", () => callApi("GET", resource.endpoint))}>Listar todos</button></div><p>GET <code>/api/{resource.endpoint}</code></p></article>
        <article className="card"><h2>Consultar por ID</h2><p>GET <code>/ById</code></p><div className="inline"><input aria-label="ID do registro" value={id} onChange={(event) => setId(event.target.value)} placeholder="00000000-0000-0000-0000-000000000000" /><button disabled={loading || !id} onClick={() => void execute("Consulta por ID", () => callApi("GET", `${resource.endpoint}/ById`, id))}>Consultar</button></div></article>
        <article className="card filter-card"><h2>Filtrar registros</h2><p>GET <code>/ByFilter</code></p><div className="fields">{resource.filterFields.map((field) => <FieldInput key={field.key} field={field} value={filters[field.key] ?? ""} onChange={(value) => change(setFilters, field.key, value)} />)}</div><button disabled={loading} onClick={() => void execute("Consulta filtrada", () => callApi("GET", `${resource.endpoint}/ByFilter`, normalize(filters, resource.filterFields)))}>Aplicar filtro</button></article>
        <FormCard title="Cadastrar registro" method="POST" fields={resource.fields} values={values} loading={loading} onChange={(key, value) => change(setValues, key, value)} onSubmit={submit("POST")} />
        <FormCard title="Atualizar registro" method="PUT" fields={[uuid, ...resource.fields]} values={values} loading={loading} onChange={(key, value) => change(setValues, key, value)} onSubmit={submit("PUT")} />
        <article className="card delete-card"><h2>Excluir registro</h2><p>DELETE <code>/api/{resource.endpoint}</code></p><div className="inline"><input aria-label="ID para exclusão" value={id} onChange={(event) => setId(event.target.value)} placeholder="ID do registro" /><button className="danger" disabled={loading || !id} onClick={() => void execute("Exclusão", () => callApi("DELETE", resource.endpoint, id))}>Excluir</button></div></article>
      </div>
      <section className="response"><div><h2>Retorno da API</h2><span>{resultRows.length ? `${resultRows.length} registro(s)` : "Sem dados retornados"}</span></div>{columns.length ? <div className="table-wrap"><table><thead><tr>{columns.map((column) => <th key={column}>{column}</th>)}</tr></thead><tbody>{resultRows.map((row, index) => <tr key={index}>{columns.map((column) => <td key={column}>{formatValue((row as Record<string, unknown>)[column])}</td>)}</tr>)}</tbody></table></div> : <pre>{result === null ? "Execute uma operação para ver a resposta." : JSON.stringify(result, null, 2)}</pre>}</section>
    </section>
  </main>;
}

function FieldInput({ field, value, onChange }: { field: Field; value: string; onChange: (value: string) => void }) { return <label><span>{field.label}{field.required ? " *" : ""}</span><input type={field.type === "uuid" ? "text" : field.type} value={value} required={field.required} min={field.min} step={field.step} maxLength={field.key === "abreviacao" ? 4 : undefined} placeholder={field.type === "uuid" ? "UUID" : undefined} onChange={(event) => onChange(event.target.value)} /></label>; }
function FormCard({ title, method, fields, values, loading, onChange, onSubmit }: { title: string; method: string; fields: Field[]; values: Record<string, string>; loading: boolean; onChange: (key: string, value: string) => void; onSubmit: (event: FormEvent) => void }) { return <article className="card form-card"><h2>{title}</h2><p>{method} <code>/{method === "POST" ? "" : ""}</code></p><form onSubmit={onSubmit}><div className="fields">{fields.map((field) => <FieldInput key={field.key} field={field} value={values[field.key] ?? ""} onChange={(value) => onChange(field.key, value)} />)}</div><button disabled={loading} type="submit">{method === "POST" ? "Cadastrar" : "Salvar alterações"}</button></form></article>; }
function formatValue(value: unknown) { if (value === null || value === undefined) return "—"; if (typeof value === "object") return JSON.stringify(value); return String(value); }
