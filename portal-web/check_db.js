const { createClient } = require('@supabase/supabase-js');

const supabaseUrl = 'https://vbnuqgyekcqxmfqiihjw.supabase.co';
const supabaseKey = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZibnVxZ3lla2NxeG1mcWlpaGp3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzI1ODAwNTEsImV4cCI6MjA4ODE1NjA1MX0.PXiA-LED8v5WMg4KwR-d1mdNycn4lKu18pDWmpryStA';

const supabase = createClient(supabaseUrl, supabaseKey);

async function checkMachines() {
    const { data, error } = await supabase.from('maquinas').select('*');
    if (error) {
        console.error('Erro:', error);
    } else {
        console.log('Máquinas encontradas no Banco:', data.length);
        data.forEach(m => {
            console.log(`- Nome: ${m.nome_maquina} | MAC: ${m.id} | Temp: ${m.temperatura_cpu} | Status: ${m.status}`);
        });
    }
}

checkMachines();
