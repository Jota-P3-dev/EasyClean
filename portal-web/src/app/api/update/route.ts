import { NextResponse } from 'next/server';

// Versão atual do Agent (Isso pode ser puxado de um banco de dados ou env var)
const LATEST_VERSION = "1.0.1";
const DOWNLOAD_URL = "https://seu-dominio.com/downloads/EasyCleanAgent.exe";

export async function GET(request: Request) {
    const { searchParams } = new URL(request.url);
    const currentVersion = searchParams.get('v');

    // Lógica simples: Se a versão enviada for diferente da atual, sugere update
    if (currentVersion !== LATEST_VERSION) {
        return NextResponse.json({
            update_available: true,
            latest_version: LATEST_VERSION,
            download_url: DOWNLOAD_URL,
            changelog: "Melhorias de performance e correções de bugs."
        });
    }

    return NextResponse.json({ update_available: false });
}
