'use client'

import { useEffect, useState } from 'react'
import { supabase } from '@/lib/supabase'
import {
  Server,
  Thermometer,
  HardDrive,
  Clock,
  Wifi,
  WifiOff,
  Activity,
  AlertTriangle,
  X,
  Building,
  Plus
} from 'lucide-react'

type Company = {
  id: string
  nome: string
  cnpj: string
}

type Machine = {
  id: string
  empresa_id: string
  nome_maquina: string // Agora será o Usuário logado
  hostname: string // Novo campo (Hostname da máquina)
  sistema_operacional: string
  status: 'ONLINE' | 'OFFLINE' | 'AGUARDANDO'
  temperatura_cpu: string
  saude_disco: string
  ultima_conexao: string
  system_uptime: string
  is_network_connected: boolean
  modelo_cpu: string
  ram_total: string
  modelo_maquina_hw: string
  fabricante: string
  uso_cpu: string
  uso_ram: string
}

type Log = {
  id: string
  acao: string
  detalhes: string
  status: string
  comando_comando: string
  created_at: string
}

export default function Dashboard() {
  const [companies, setCompanies] = useState<Company[]>([])
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('all')
  const [machines, setMachines] = useState<Machine[]>([])
  const [loading, setLoading] = useState(true)
  const [selectedMachine, setSelectedMachine] = useState<Machine | null>(null)
  const [logs, setLogs] = useState<Log[]>([])
  const [loadingLogs, setLoadingLogs] = useState(false)
  const [selectedLog, setSelectedLog] = useState<Log | null>(null)

  // Modals
  const [showCompanyModal, setShowCompanyModal] = useState(false)
  const [newCompanyName, setNewCompanyName] = useState('')
  const [newCompanyCnpj, setNewCompanyCnpj] = useState('')

  // Força re-render a cada 30 segundos
  const [, setTick] = useState(0)

  useEffect(() => {
    const interval = setInterval(() => setTick(t => t + 1), 30000)
    return () => clearInterval(interval)
  }, [])

  useEffect(() => {
    fetchCompanies()
    fetchMachines()

    const subscription = supabase
      .channel('maquinas_changes')
      .on(
        'postgres_changes',
        { event: '*', schema: 'public', table: 'maquinas' },
        (payload) => {
          fetchMachines()
        }
      )
      .subscribe()

    return () => {
      supabase.removeChannel(subscription)
    }
  }, [])

  async function fetchCompanies() {
    const { data, error } = await supabase.from('empresas').select('*').order('nome')
    if (data) setCompanies(data as Company[])
  }

  async function fetchMachines() {
    try {
      const { data, error } = await supabase
        .from('maquinas')
        .select('*')
        .order('ultima_conexao', { ascending: false })

      if (error) throw error
      if (data) setMachines(data as Machine[])
    } catch (error: any) {
      console.error('Erro buscando máquinas:', error)
    } finally {
      setLoading(false)
    }
  }

  async function fetchLogs(machineId: string) {
    setLoadingLogs(true)
    setLogs([])
    try {
      const { data, error } = await supabase
        .from('logs')
        .select('*')
        .eq('maquina_id', machineId)
        .order('created_at', { ascending: false })
        .limit(10)

      if (error) throw error;
      if (data) setLogs(data as Log[])
    } catch (error: any) {
      console.error('Erro detalhado buscando logs:', error);
    } finally {
      setLoadingLogs(false)
    }
  }

  async function handleCreateCompany() {
    if (!newCompanyName) return alert('O nome da empresa é obrigatório.')

    // UUID v4 simples para o frontend
    const newId = crypto.randomUUID()

    const { error } = await supabase.from('empresas').insert([{
      id: newId,
      nome: newCompanyName,
      cnpj: newCompanyCnpj || '00.000.000/0000-00'
    }])

    if (error) {
      alert('Erro ao criar empresa: ' + error.message)
    } else {
      alert('Empresa criada com sucesso!')
      setShowCompanyModal(false)
      setNewCompanyName('')
      setNewCompanyCnpj('')
      fetchCompanies()
      setSelectedCompanyId(newId)
    }
  }

  function handleOpenDetails(m: Machine) {
    setSelectedMachine(m)
    fetchLogs(m.id)
  }

  async function requestCleaning(machineId: string) {
    const { error } = await supabase
      .from('comandos')
      .insert([{ maquina_id: machineId, comando: 'LIMPEZA_COMPLETA', status: 'PENDENTE' }])
    if (error) alert("Erro ao enviar comando: " + error.message)
    else alert("Comando de limpeza enviado para o Agente!")
  }

  const getMachineStatus = (m: Machine) => {
    if (!m.ultima_conexao) return 'OFFLINE'
    const lastConn = new Date(m.ultima_conexao).getTime()
    const now = new Date().getTime()
    const diff = now - lastConn
    return (diff <= 90000 && m.status === 'ONLINE') ? 'ONLINE' : 'OFFLINE'
  }

  const isHot = (tempStr: string) => {
    if (!tempStr || tempStr === "Desconhecida" || tempStr === "N/A") return false
    const t = parseFloat(tempStr.replace('°C', ''))
    return t > 80
  }

  // Filtragem local
  const displayedMachines = selectedCompanyId === 'all'
    ? machines
    : machines.filter(m => m.empresa_id === selectedCompanyId)

  return (
    <div className="min-h-screen bg-slate-950 text-slate-200 p-8 font-sans">

      <header className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-white flex items-center gap-3">
            <Activity className="text-emerald-500" />
            EasyClean Portal
          </h1>
          <p className="text-slate-400 mt-1">Gerenciamento Centralizado de Agentes</p>
        </div>
        <div className="flex gap-4">
          <div className="bg-slate-900 px-4 py-2 rounded-lg border border-slate-800 text-sm flex flex-col justify-center">
            <span className="text-slate-400 text-xs">Empresas</span>
            <span className="font-bold text-emerald-400">{companies.length}</span>
          </div>
          <div className="bg-slate-900 px-4 py-2 rounded-lg border border-slate-800 text-sm flex flex-col justify-center">
            <span className="text-slate-400 text-xs">Máquinas Online</span>
            <span className="font-bold text-emerald-400">
              {machines.filter(m => getMachineStatus(m) === 'ONLINE').length}
            </span>
          </div>
        </div>
      </header>

      {/* Barre de Filtro de Empresa */}
      <div className="mb-8 flex flex-col sm:flex-row items-center gap-4 bg-slate-900 border border-slate-800 p-4 rounded-xl">
        <div className="flex-1 w-full">
          <label className="text-[10px] font-bold text-slate-500 uppercase tracking-widest pl-1">Filtrar por Empresa</label>
          <div className="relative mt-1">
            <Building className="absolute left-3 top-2.5 text-slate-500" size={16} />
            <select
              className="w-full bg-slate-950 border border-slate-700 text-slate-200 rounded-lg py-2 pl-10 pr-4 appearance-none outline-none focus:border-emerald-500 transition-colors"
              value={selectedCompanyId}
              onChange={e => setSelectedCompanyId(e.target.value)}
            >
              <option value="all">Todas as Máquinas (Geral)</option>
              {companies.map(c => (
                <option key={c.id} value={c.id}>{c.nome} {c.cnpj ? `(${c.cnpj})` : ''}</option>
              ))}
            </select>
          </div>
        </div>
        <div className="w-full sm:w-auto pt-0 sm:pt-5">
          <button
            onClick={() => setShowCompanyModal(true)}
            className="w-full bg-emerald-600/10 hover:bg-emerald-600/20 text-emerald-500 border border-emerald-500/30 hover:border-emerald-500 font-medium py-2 px-4 rounded-lg transition-all flex items-center justify-center gap-2"
          >
            <Plus size={16} /> Nova Empresa
          </button>
        </div>
      </div>

      {loading ? (
        <div className="text-center text-slate-500 py-20 animate-pulse">
          Sincronizando com o Supabase...
        </div>
      ) : displayedMachines.length === 0 ? (
        <div className="text-center py-20 bg-slate-900/50 rounded-xl border border-dashed border-slate-800">
          <Server className="mx-auto h-12 w-12 text-slate-600 mb-3" />
          <h3 className="text-lg font-medium text-slate-300">
            {selectedCompanyId === 'all' ? 'Nenhuma máquina conectada' : 'Nenhuma máquina nesta empresa'}
          </h3>
          <p className="text-slate-500 text-sm mt-1">
            Instale o EasyClean Agent nos computadores clientes ou selecione outra empresa.
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
          {displayedMachines.map((machine) => (
            <div
              key={machine.id}
              onClick={() => handleOpenDetails(machine)}
              className="bg-slate-900 rounded-xl p-6 border border-slate-800 hover:border-emerald-500/50 hover:shadow-lg hover:shadow-emerald-500/5 transition-all cursor-pointer relative overflow-hidden group"
            >
              {isHot(machine.temperatura_cpu) && (
                <div className="absolute top-0 left-0 right-0 bg-red-500 text-white px-4 py-1 text-[10px] font-bold uppercase tracking-wider flex items-center justify-center gap-2 z-10">
                  <AlertTriangle size={12} />
                  Manutenção Sugerida
                </div>
              )}

              <div className={`flex items-start justify-between ${isHot(machine.temperatura_cpu) ? 'mt-4' : ''}`}>
                <div>
                  <h2 className="text-xl font-semibold text-white group-hover:text-emerald-400 transition-colors flex items-center gap-2" title="Usuário Logado">
                    {machine.nome_maquina}
                  </h2>
                  <p className="text-slate-500 text-xs mt-1" title="Hostname">
                    🖥️ {machine.hostname || 'Hostname Pendente'}
                  </p>
                  <p className="text-slate-600 text-[10px] mt-1">
                    {machine.fabricante !== 'Desconhecido' ? `${machine.fabricante} - ` : ''}{machine.modelo_maquina_hw}
                  </p>
                </div>

                <span className={`px-2 py-1 rounded-md text-[10px] font-bold border shrink-0 ${getMachineStatus(machine) === 'ONLINE'
                  ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20'
                  : 'bg-slate-800 text-slate-400 border-slate-700'
                  }`}>
                  {getMachineStatus(machine)}
                </span>
              </div>

              <div className="mt-6 grid grid-cols-1 gap-5">
                {/* CPU usage section */}
                <div className="space-y-2">
                  <div className="flex justify-between items-center">
                    <p className="text-slate-500 text-[10px] uppercase font-bold tracking-tight">CPU</p>
                    <p className={`text-xs font-bold ${isHot(machine.temperatura_cpu) ? 'text-red-400' : 'text-emerald-400'}`}>
                      {machine.uso_cpu || '0%'}
                    </p>
                  </div>
                  <div className="h-1.5 w-full bg-slate-800 rounded-full overflow-hidden">
                    <div
                      className={`h-full transition-all duration-1000 ${parseFloat(machine.uso_cpu?.replace('%', '') || '0') > 85 ? 'bg-red-500' :
                          parseFloat(machine.uso_cpu?.replace('%', '') || '0') > 60 ? 'bg-amber-500' : 'bg-emerald-500'
                        }`}
                      style={{ width: machine.uso_cpu || '0%' }}
                    ></div>
                  </div>
                  <p className="text-[10px] text-slate-500 leading-none">{machine.temperatura_cpu}</p>
                </div>

                {/* RAM usage section */}
                <div className="space-y-2">
                  <div className="flex justify-between items-center">
                    <p className="text-slate-500 text-[10px] uppercase font-bold tracking-tight">RAM / DISCO</p>
                    <p className="text-xs font-bold text-slate-200">{machine.uso_ram || '0%'}</p>
                  </div>
                  <div className="h-1.5 w-full bg-slate-800 rounded-full overflow-hidden">
                    <div
                      className={`h-full transition-all duration-1000 ${parseFloat(machine.uso_ram?.replace('%', '') || '0') > 85 ? 'bg-red-500' :
                          parseFloat(machine.uso_ram?.replace('%', '') || '0') > 60 ? 'bg-amber-500' : 'bg-slate-400'
                        }`}
                      style={{ width: machine.uso_ram || '0%' }}
                    ></div>
                  </div>
                  <p className="text-[10px] text-slate-500 leading-none">{machine.saude_disco}</p>
                </div>
              </div>

              <div className="mt-6 flex items-center justify-between text-[11px] text-slate-500 pt-4 border-t border-slate-800/50">
                <span className="flex items-center gap-1">
                  <Clock size={12} /> {machine.system_uptime || 'N/A'}
                </span>
                <span className="opacity-0 group-hover:opacity-100 transition-opacity text-emerald-500 font-bold">
                  Ver Detalhes →
                </span>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Modal Nova Empresa */}
      {showCompanyModal && (
        <div className="fixed inset-0 z-[60] flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
          <div className="bg-slate-900 border border-slate-800 rounded-xl w-full max-w-md shadow-2xl overflow-hidden animate-in fade-in zoom-in-95 duration-200">
            <div className="p-4 border-b border-slate-800 flex justify-between items-center bg-slate-900/50">
              <h2 className="text-lg font-bold text-white flex items-center gap-2">
                <Building className="text-emerald-500" size={20} />
                Cadastrar Nova Empresa
              </h2>
              <button
                onClick={() => setShowCompanyModal(false)}
                className="text-slate-500 hover:text-white transition-colors"
              >
                <X size={20} />
              </button>
            </div>

            <div className="p-6 space-y-4">
              <div>
                <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest mb-1">Nome da Instituição</label>
                <input
                  type="text"
                  autoFocus
                  placeholder="Ex: XYZ Corp"
                  className="w-full bg-slate-950 border border-slate-700 text-slate-200 rounded-lg p-3 outline-none focus:border-emerald-500 transition-colors"
                  value={newCompanyName}
                  onChange={e => setNewCompanyName(e.target.value)}
                />
              </div>
              <div>
                <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest mb-1">CNPJ / Documento (Opcional)</label>
                <input
                  type="text"
                  placeholder="00.000.000/0001-00"
                  className="w-full bg-slate-950 border border-slate-700 text-slate-200 rounded-lg p-3 outline-none focus:border-emerald-500 transition-colors"
                  value={newCompanyCnpj}
                  onChange={e => setNewCompanyCnpj(e.target.value)}
                />
              </div>
            </div>

            <div className="p-4 border-t border-slate-800 bg-slate-900/50 flex justify-end gap-3">
              <button
                onClick={() => setShowCompanyModal(false)}
                className="px-4 py-2 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors font-medium"
              >
                Cancelar
              </button>
              <button
                onClick={handleCreateCompany}
                className="px-6 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg font-bold shadow-lg shadow-emerald-500/20 transition-all"
              >
                Salvar Empresa
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal de Detalhes da Máquina */}
      {selectedMachine && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl w-full max-w-4xl max-h-[90vh] overflow-hidden flex flex-col shadow-2xl">

            <div className="p-6 border-b border-slate-800 flex items-center justify-between bg-slate-900">
              <div className="flex items-center gap-4">
                <div className="w-12 h-12 rounded-xl bg-emerald-500/10 flex items-center justify-center">
                  <Server className="text-emerald-500" size={24} />
                </div>
                <div>
                  <h2 className="text-2xl font-bold text-white leading-tight">{selectedMachine.nome_maquina} (Usuário)</h2>
                  <p className="text-slate-400 text-sm">Hostname: <strong className="text-slate-300">{selectedMachine.hostname || 'Pendente'}</strong> | ID: {selectedMachine.id}</p>
                </div>
              </div>
              <button
                onClick={() => setSelectedMachine(null)}
                className="text-slate-500 hover:text-white transition-colors"
              >
                <X size={24} />
              </button>
            </div>

            <div className="flex-1 min-h-0 overflow-hidden flex flex-col lg:flex-row p-4 lg:p-8 gap-6 lg:gap-8">
              {/* Coluna 1: Hardware (Fixa) */}
              <div className="flex flex-col gap-4 w-full lg:w-[45%] shrink-0 overflow-hidden">
                <h3 className="text-[10px] font-bold uppercase text-slate-500 tracking-widest shrink-0">Especificações Técnicas</h3>
                <div className="bg-slate-950 rounded-xl p-4 border border-slate-800 space-y-3 shrink-0">
                  <div className="flex justify-between items-center pb-2 border-b border-white/5">
                    <span className="text-slate-500 text-xs">Fabricante</span>
                    <span className="text-slate-200 text-xs font-medium">{selectedMachine.fabricante}</span>
                  </div>
                  <div className="flex justify-between items-center pb-2 border-b border-white/5">
                    <span className="text-slate-500 text-xs">Modelo</span>
                    <span className="text-slate-200 text-xs font-medium truncate ml-4" title={selectedMachine.modelo_maquina_hw}>{selectedMachine.modelo_maquina_hw}</span>
                  </div>
                  <div className="flex flex-col gap-1 pb-2 border-b border-white/5">
                    <div className="flex justify-between items-center">
                      <span className="text-slate-500 text-xs">Processador</span>
                      <div className="text-right">
                        <p className="text-emerald-400 text-sm font-bold leading-tight">{selectedMachine.uso_cpu || '0%'}</p>
                        <p className="text-[9px] text-slate-500">{selectedMachine.temperatura_cpu}</p>
                      </div>
                    </div>
                    <span className="text-slate-400 text-[10px] truncate" title={selectedMachine.modelo_cpu}>{selectedMachine.modelo_cpu}</span>
                  </div>
                  <div className="flex justify-between items-center pb-2 border-b border-white/5">
                    <span className="text-slate-500 text-xs">RAM</span>
                    <div className="text-right">
                      <p className="text-slate-200 text-xs font-medium leading-tight">{selectedMachine.uso_ram || '0%'}</p>
                      <p className="text-[9px] text-slate-500">{selectedMachine.ram_total}</p>
                    </div>
                  </div>
                  <div className="flex justify-between items-center pb-2 border-b border-white/5">
                    <span className="text-slate-500 text-xs">Disco</span>
                    <span className="text-slate-200 text-xs font-medium text-right">{selectedMachine.saude_disco}</span>
                  </div>
                  <div className="flex justify-between items-center">
                    <span className="text-slate-500 text-xs">Sistema</span>
                    <span className="text-slate-200 text-xs font-medium text-right truncate ml-4" title={selectedMachine.sistema_operacional}>{selectedMachine.sistema_operacional}</span>
                  </div>
                </div>

                <div className="mt-auto space-y-2 shrink-0">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      requestCleaning(selectedMachine.id);
                    }}
                    className="w-full bg-emerald-600 hover:bg-emerald-500 text-white font-bold py-3 rounded-xl transition-all shadow-lg shadow-emerald-600/10 flex items-center justify-center gap-2 text-sm"
                  >
                    <Activity size={18} />
                    DISPARAR MANUTENÇÃO
                  </button>
                  <p className="text-center text-[10px] text-slate-500 leading-tight">
                    Inicia limpeza remota profunda se online.
                  </p>
                </div>
              </div>

              {/* Coluna 2: Logs (Somente Scroll) */}
              <div className="flex flex-col gap-4 flex-1 min-h-[250px] lg:min-h-0 overflow-hidden">
                <h3 className="text-[10px] font-bold uppercase text-slate-500 tracking-widest shrink-0">Histórico de Atividades</h3>
                <div className="bg-slate-950 rounded-xl border border-slate-800 flex-1 overflow-y-auto custom-scrollbar">
                  {loadingLogs ? (
                    <div className="flex-1 flex items-center justify-center text-slate-600 animate-pulse text-sm">Carregando histórico...</div>
                  ) : logs.length === 0 ? (
                    <div className="flex-1 flex flex-col items-center justify-center text-slate-600 p-8 text-center space-y-2">
                      <Activity size={32} className="opacity-20" />
                      <p className="text-sm">Nenhum log encontrado para esta máquina.</p>
                    </div>
                  ) : (
                    <div className="divide-y divide-slate-800 overflow-hidden">
                      {logs.map(log => (
                        <div
                          key={log.id}
                          className="p-4 hover:bg-slate-900/50 transition-colors cursor-pointer group"
                          onClick={() => setSelectedLog(log)}
                        >
                          <div className="flex justify-between items-start mb-1">
                            <span className={`text-[10px] font-bold px-1.5 py-0.5 rounded ${log.status === 'CONCLUIDO' ? 'bg-emerald-500/20 text-emerald-400' : 'bg-red-500/20 text-red-400'
                              }`}>
                              {log.acao} ({log.status})
                            </span>
                            <span className="text-[10px] text-slate-600 font-mono group-hover:text-emerald-500 transition-colors flex items-center gap-1">
                              VER DETALHES
                            </span>
                          </div>
                          <p className="text-xs text-slate-400 line-clamp-1 mt-2 leading-relaxed opacity-80">
                            {new Date(log.created_at).toLocaleString('pt-BR')}
                          </p>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            </div>

          </div>
        </div>
      )}

      {selectedLog && (
        <div className="fixed inset-0 z-[70] flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm">
          <div className="bg-slate-900 border border-slate-800 rounded-xl max-w-2xl w-full shadow-2xl overflow-hidden animate-in fade-in zoom-in-95 duration-200">
            <div className="p-4 border-b border-slate-800 flex justify-between items-center bg-slate-900/50">
              <h2 className="text-sm font-bold text-white flex items-center gap-2">
                <span className={`w-2 h-2 rounded-full ${selectedLog.status === 'CONCLUIDO' ? 'bg-emerald-500' : 'bg-red-500'}`}></span>
                Detalhes da Execução ({selectedLog.status})
              </h2>
              <button
                onClick={() => setSelectedLog(null)}
                className="p-1 hover:bg-slate-800 rounded-lg text-slate-400 transition-colors"
              >
                <X size={16} />
              </button>
            </div>

            <div className="p-6">
              <div className="mb-4 text-xs text-slate-500 flex justify-between border-b border-slate-800 pb-3">
                <span>Data: <strong className="text-slate-300 font-mono">{new Date(selectedLog.created_at).toLocaleString('pt-BR')}</strong></span>
                <span>Ação: <strong className="text-slate-300">{selectedLog.acao}</strong></span>
              </div>

              <div className="bg-black/50 border border-slate-800 rounded-lg p-4 max-h-[400px] overflow-y-auto">
                <pre className="text-xs text-slate-300 font-mono whitespace-pre-wrap break-words leading-relaxed">
                  {selectedLog.detalhes || 'Nenhum detalhe gerado para esta execução.'}
                </pre>
              </div>
            </div>
          </div>
        </div>
      )}

    </div>
  )
}
