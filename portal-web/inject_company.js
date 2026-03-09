const { createClient } = require('@supabase/supabase-js');

const supabaseUrl = 'https://vbnuqgyekcqxmfqiihjw.supabase.co';
const supabaseKey = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InZibnVxZ3lla2NxeG1mcWlpaGp3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzI1ODAwNTEsImV4cCI6MjA4ODE1NjA1MX0.PXiA-LED8v5WMg4KwR-d1mdNycn4lKu18pDWmpryStA';

const supabase = createClient(supabaseUrl, supabaseKey);

async function injectFakeCompany() {
    const { data, error } = await supabase
        .from('empresas')
        .upsert({
            id: '11111111-1111-1111-1111-111111111111',
            nome: 'Empresa Teste EasyClean',
            cnpj: '00.000.000/0001-00'
        });

    if (error) {
        console.error('Erro ao inserir empresa:', error);
    } else {
        console.log('Empresa injetada com sucesso!');
    }
}

injectFakeCompany();
