const { createClient } = require('@supabase/supabase-js');

const supabaseUrl = 'https://vbnuqgyekcqxmfqiihjw.supabase.co';
const supabaseKey = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZibnVxZ3lla2NxeG1mcWlpaGp3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzI1ODAwNTEsImV4cCI6MjA4ODE1NjA1MX0.PXiA-LED8v5WMg4KwR-d1mdNycn4lKu18pDWmpryStA';

const supabase = createClient(supabaseUrl, supabaseKey);

async function checkDatabase() {
    console.log("=== CHECKING MACHINES ===");
    const { data: machines, error: mError } = await supabase.from('maquinas').select('id, nome_maquina, hostname, status, ultima_conexao');
    if (mError) console.error("Machines Error:", mError);
    else console.dir(machines);

    console.log("\n=== CHECKING COMMANDS ===");
    const { data: commands, error: cError } = await supabase.from('comandos').select('*').order('criado_em', { ascending: false }).limit(5);
    if (cError) console.error("Commands Error:", cError);
    else console.dir(commands);

    console.log("\n=== CHECKING LOGS ===");
    const { data: logs, error: lError } = await supabase.from('logs').select('*').order('created_at', { ascending: false }).limit(5);
    if (lError) console.error("Logs Error:", lError);
    else console.dir(logs);
}

checkDatabase();
