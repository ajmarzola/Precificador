import { NextRequest, NextResponse } from "next/server";
import http from "node:http";
import https from "node:https";

export const dynamic = "force-dynamic";

const baseUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

function requestApi(method: string, path: string, body?: string): Promise<{ status: number; text: string }> {
  const url = new URL(path, baseUrl);
  const client = url.protocol === "https:" ? https : http;

  return new Promise((resolve, reject) => {
    const apiRequest = client.request(url, {
      method,
      headers: body ? { "content-type": "application/json", "content-length": Buffer.byteLength(body) } : undefined
    }, (response) => {
      let text = "";
      response.setEncoding("utf8");
      response.on("data", (chunk) => { text += chunk; });
      response.on("end", () => resolve({ status: response.statusCode ?? 502, text }));
    });
    apiRequest.on("error", reject);
    if (body) apiRequest.write(body);
    apiRequest.end();
  });
}

async function proxy(request: NextRequest, context: { params: Promise<{ path: string[] }> }) {
  const { path } = await context.params;
  const payload = request.method === "GET" ? request.nextUrl.searchParams.get("body") : await request.text();

  try {
    const result = await requestApi(request.method, `/api/${path.join("/")}`, payload || undefined);
    return new NextResponse(result.text, {
      status: result.status,
      headers: { "content-type": "application/json; charset=utf-8" }
    });
  } catch {
    return NextResponse.json({ message: "Não foi possível conectar à Precificador.WebApi. Verifique se ela está em execução." }, { status: 502 });
  }
}

export const GET = proxy;
export const POST = proxy;
export const PUT = proxy;
export const DELETE = proxy;
