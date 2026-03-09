const { createClient } = require('@supabase/supabase-js');

const supabaseUrl = 'https://vbnuqgyekcqxmfqiihjw.supabase.co';
const supabaseKey = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZibnVxZ3lla2NxeG1mcWlpaGp3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzI1ODAwNTEsImV4cCI6MjA4ODE1NjA1MX0.PXiA-LED8v5WMg4KwR-d1mdNycn4lKu18pDWmpryStA';

const supabase = createClient(supabaseUrl, supabaseKey);

async function fixDatabase() {
    console.log("=== ADICIONANDO COLUNA HOSTNAME ===");
    // O Supabase JS normal não permite fazer ALTER TABLE, 
    // precisamos executar via função RPC se exisir ou via HTTP.
    // Como não temos acesso root ao Postgres aqui (apenas chave anon), 
    // avisarei o usuário que ele precisa alterar a tabela, pois erro 42703 ocorreu.
    console.log("Atenção: A coluna 'hostname' não existe na tabela 'maquinas'.");
    console.log("Como estamos usando API REST (anon key), não podemos criar a coluna via JS.");
}

fixDatabase();
