const flow = document.querySelector('#flow');
const start = document.querySelector('#start');
const status = document.querySelector('#status');
const steps = document.querySelector('#steps');
const empty = document.querySelector('#empty');
const subtitle = document.querySelector('#activity-subtitle');
const error = document.querySelector('#error');
const requestError = document.querySelector('#request-error');
const outputDir = document.querySelector('#output-dir');
const results = document.querySelector('#results');
const json = document.querySelector('#json');
const descriptions = { demo: 'Assinatura, envio, consulta e protocolo simulados.', schemas: 'Confere os arquivos e compila os schemas fiscais.' };
flow.addEventListener('change', () => document.querySelector('#flow-description').textContent = descriptions[flow.value] || 'Prepara uma venda de exemplo e gera um DANFE sintético.');
start.addEventListener('click', async () => {
  start.disabled = true;
  requestError.textContent = '';
  try {
    const response = await fetch(`/api/execution/${flow.value}`, { method: 'POST' });
    if (!response.ok) { const body = await response.json(); throw new Error(body.message); }
    await refresh();
  } catch (e) { requestError.textContent = e.message || 'Não foi possível iniciar a execução.'; start.disabled = false; }
});
document.querySelector('#copy').addEventListener('click', async event => {
  try { await navigator.clipboard.writeText(json.textContent); event.currentTarget.textContent = 'Copiado'; setTimeout(() => event.target.textContent = 'Copiar JSON', 2000); }
  catch { event.currentTarget.textContent = 'Selecione o texto para copiar'; }
});
const names = { idle: 'PRONTO', running: 'EM ANDAMENTO', succeeded: 'CONCLUÍDO', failed: 'FALHOU' };
async function refresh() {
  try {
    const response = await fetch('/api/execution', { cache: 'no-store' });
    if (!response.ok) throw new Error('Falha na consulta');
    const data = await response.json();
    status.className = `status ${data.status}`;
    status.replaceChildren(document.createElement('span'), document.createTextNode(' ' + names[data.status]));
    start.disabled = data.status === 'running'; flow.disabled = data.status === 'running';
    empty.hidden = data.steps.length > 0; steps.hidden = data.steps.length === 0;
    steps.replaceChildren(...data.steps.map((step, index) => {
      const li = document.createElement('li');
      const marker = document.createElement('span'); marker.className = 'step-marker'; marker.textContent = index + 1;
      const details = document.createElement('div'); const title = document.createElement('strong'); title.textContent = step.message;
      const time = document.createElement('small'); time.textContent = new Date(step.at).toLocaleTimeString('pt-BR');
      details.append(title, time); li.append(marker, details); return li;
    }));
    subtitle.textContent = data.startedAt ? `Iniciada às ${new Date(data.startedAt).toLocaleTimeString('pt-BR')}` : 'Aguardando uma execução.';
    error.hidden = !data.error; error.textContent = data.error || '';
    outputDir.hidden = !data.outputDirectory; outputDir.textContent = data.outputDirectory ? `Arquivos gerados em: ${data.outputDirectory}` : '';
    results.hidden = !data.result; json.textContent = data.result || '';
    requestError.textContent = '';
  } catch { requestError.textContent = 'Não foi possível conectar ao monitor. Verifique se ele está em execução.'; start.disabled = true; }
}
refresh(); setInterval(refresh, 1000);
