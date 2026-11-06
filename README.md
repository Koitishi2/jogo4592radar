# jogo4592radar 1.0.10

Aplicativo Windows 10/11 em C# e WPF com overlay transparente em formato de losango, alinhável ao minimapa. A captura passiva por Npcap decodifica eventos Photon e mostra **pontos verdes de recursos** a partir de coordenadas recebidas, com largura, altura, raio e rotação ajustáveis. Há controles separados para mostrar recursos e avisar sobre jogadores detectados: o aviso muda a borda para laranja, sem indicar posição ou confirmar que sejam inimigos. A simulação de pontos vermelhos foi removida. Posições de outros jogadores ainda não são exibidas porque não foram validadas nos dados capturados. Não injeta DLL nem acessa memória do Albion Online.

## Histórico de versões

- **1.0.10 (2026-11-06):** Separa recursos e aviso de jogadores em tempo real; borda laranja indica presença sem posição; remove recursos esgotados no evento 46. Não desenha inimigos sem coordenadas validadas.

- **1.0.9 (2026-11-06):** Ajusta posição e tamanho iniciais do losango conforme minimapa 1920x1080; adiciona botão de realinhamento e contorno visível ao configurar. Outras escalas exigem ajuste manual.

- **1.0.8 (2026-11-06):** Substitui círculo por losango alinhável ao minimapa; adiciona ajuste da altura e recorta os recursos ao mapa. Registra teste fluido da 1.0.7; inimigos seguem sem posição validada.

- **1.0.7 (2026-11-06):** Prioriza o adaptador de rede ativo na captura Npcap para reduzir trabalho; mantém fallback quando não há correspondência. Documenta teste de eventos de jogadores e melhora parcial de desempenho.

- **1.0.6 (2026-11-06):** Remove pontos simulados; desenha recursos em uma única camada visual, reduz atualizações e processamento de eventos; prévia ligada por padrão. Posições de jogadores seguem não validadas.

- **1.0.5 (2026-11-06):** Prévia opcional de recursos com posições de eventos Photon, projeção ajustável por raio e rotação; jogadores continuam sem posição validada.

- **1.0.4 (2026-11-06):** Captura passiva Npcap e contagem de eventos Photon, jogadores, recursos e movimento; marcadores permanecem simulados.

- **1.0.3 (2026-11-06):** Janela de controle ampliada com rolagem e textos de demonstração corrigidos para evitar corte em telas com escala.

- **1.0.2 (2026-11-05):** Documentadas as regras oficiais, a ausência de bypass e o limite dos testes a uma janela de demonstração.

- **1.0.1 (2026-11-05):** Nome do projeto, executável e interface alterado para jogo4592radar; teste de inicialização repetido.

- **1.0.0 (2026-11-05):** primeira base em WPF, overlay transparente com tamanho e escurecimento ajustáveis, alinhamento manual, marcadores simulados, pausa e modo de clique através. Compilação Release e teste de inicialização aprovados. A validação visual dentro do jogo ainda está pendente.

## Regra de versões

Cada nova versão compilada para distribuição avança uma posição: `1.0.0 → 1.0.1 → … → 1.0.99 → 1.1.0`. Use `release.ps1` com uma descrição da mudança; ele atualiza a versão do projeto, compila, executa o teste inicial e acrescenta a entrada acima. Revise o resultado e registre o commit no Git. Compilações de desenvolvimento com `dotnet build` não geram uma nova versão.

```powershell
.\release.ps1 -Change 'Descrição da alteração'
```

## Compilar e executar

Requer o **.NET 9 SDK** (Windows). No PowerShell, nesta pasta:

```powershell
dotnet build .\jogo4592radar.csproj -c Release
dotnet run --project .\jogo4592radar.csproj -c Release
```

O executável compilado fica em `bin\Release\net9.0-windows\jogo4592radar.exe`. Para distribuir sem exigir runtime no computador de destino:

```powershell
dotnet publish .\jogo4592radar.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Para repetir o teste automático de inicialização após compilar:

```powershell
.\test-smoke.ps1
```

## Diagnóstico Npcap

Clique em **Iniciar captura** na janela de controle. O aplicativo abre os adaptadores ativos do Npcap e filtra UDP nas portas 5055, 5056 e 5058. A janela mostra pacotes candidatos, eventos Photon decodificados e contagens separadas de entrada de jogador, recurso e movimento. **Esses contadores não são pontos no mapa nem identificam inimigos.** A captura só começa após o clique e é encerrada ao fechar a janela. O aplicativo não grava payloads em disco.

Para testar os recursos, inicie a captura **antes de entrar ou se mover** em uma área com recursos. A opção **Mostrar recursos capturados** já vem marcada; desmarque-a para ocultar os pontos. O programa só desenha recursos depois de receber a posição local por uma requisição de movimento ou resposta de entrada no mapa. **Alinhar ao minimapa** restaura uma posição calculada para a tela principal; em 1920×1080, usa aproximadamente 365 px de largura e 72% de altura. Arraste o losango para corrigir a posição e ajuste **largura**, **altura**, **raio** e **rotação** observando o alinhamento; os valores são provisórios e dependem da escala da interface do jogo. A camada de pontos é recortada ao losango. A visualização pode omitir recursos ou mostrar pontos antigos por até 60 segundos; a precisão ainda não foi medida.

Marque **Avisar sobre jogadores detectados (sem posição)** para receber um aviso visual separado: a borda do losango fica laranja enquanto há identificadores de jogadores recentes no tráfego. O aviso não informa direção, distância, equipe ou hostilidade; ele pode incluir jogadores amigáveis e permanecer até 60 segundos sem novo evento. Se a captura começar depois da entrada de um jogador, talvez não receba o evento inicial dele. Desmarque para ocultar o aviso. A opção não coloca pontos vermelhos no minimapa.

Recursos recebidos nos eventos 39/40 são desenhados com coordenadas explícitas. O evento 46 sem o campo de tamanho remove o recurso esgotado imediatamente, além da remoção anterior por saída de entidade (evento 1) e prazo máximo de 60 segundos. A interpretação do evento 46 segue o [tratamento documentado no OpenRadar](https://github.com/Nouuu/Albion-Online-OpenRadar/blob/main/web/scripts/handlers/HarvestablesHandler.js).

Em um teste local de 12 segundos com o cliente aberto (2026-11-06), a captura abriu 5 adaptadores, observou 367 pacotes UDP candidatos, decodificou 580 eventos, incluindo 9 de entrada de jogador e 476 de movimento; 0 eventos de recurso apareceram nesse intervalo e houve 0 erros de decodificação. A origem de cada pacote não foi atribuída a um processo, e esses números não validam coordenadas nem a exibição no minimapa.

Em outra captura de 20 segundos com o personagem se movendo (2026-11-06), chegaram requisições de movimento com posição local e 33 eventos de recursos, incluindo lotes com coordenadas e recursos individuais. Um teste de projeção com valores dessa captura produziu três pontos verdes dentro do raio configurado. Uma captura posterior com o personagem parado não recebeu nova posição local, portanto a prévia ficou vazia como esperado.

**Teste manual da 1.0.5 (2026-11-06):** com o jogo e a captura abertos, o diagnóstico mostrou recursos rastreados e dentro do raio. A prévia estava desmarcada; após marcá-la e mover o personagem, o usuário confirmou que apareceram pontos verdes e que eles coincidiam aproximadamente com recursos visíveis no minimapa. Isso valida a exibição básica nessa sessão, mas ainda não mede precisão, atualização após coleta, mudanças de mapa ou posicionamento em outras resoluções. Após reiniciar o aplicativo, é necessário iniciar a captura novamente e obter uma nova posição local por movimento ou entrada no mapa.

O [OpenRadar](https://github.com/Nouuu/Albion-Online-OpenRadar/blob/main/web/scripts/drawings/PlayersDrawing.js) registra que o desenho de posições de jogadores está desativado porque essas posições chegam cifradas. Também observamos eventos de movimento com posições em bytes, sem coordenadas verificadas. Por isso, a presença de um jogador detectada no tráfego não é convertida em marcador vermelho.

**Jogadores:** mesmo com outros jogadores ou inimigos no mapa, o aplicativo não desenha pontos vermelhos. O evento de entrada confirma apenas presença; não fornece uma posição utilizável. A partir da 1.0.10, identificadores recentes podem acionar uma borda laranja opcional. Essa limitação de posicionamento permanece até uma validação independente de coordenadas obtidas sem acessar a memória do jogo. A [análise técnica do OpenRadar](https://github.com/Nouuu/Albion-Online-OpenRadar/blob/main/docs/technical/PLAYER_POSITIONS_MITM.md) descreve as posições dos jogadores nos eventos 29 e 3 como cifradas e conclui que a captura passiva não consegue colocá-los no mapa. Não foi implementado proxy ou alteração do caminho de rede.

**Amostra adicional (2026-11-06):** uma coleta passiva de 15 segundos com outros jogadores visíveis recebeu 3 eventos de entrada (código 29) e 64 de movimento (código 3). Registramos apenas os tipos e tamanhos dos campos, sem nomes ou conteúdo. O evento de movimento continha um identificador e um bloco de 30 bytes; não foi possível validar duas coordenadas de posição nesse bloco. As capturas de tela mostram jogadores na cena, mas não fornecem uma correspondência entre esses eventos e posições no minimapa. Portanto, pontos vermelhos continuam ausentes por decisão de precisão.

**Desempenho da 1.0.6:** cada atualização desenha todos os recursos em uma única camada visual, sem criar um elemento WPF para cada ponto. A animação e o timer de 80 ms da simulação foram removidos; a atualização do diagnóstico passou de 500 ms para 750 ms; eventos sem relação com recursos deixam de entrar no rastreador. No teste de abertura sem captura, o processo consumiu 0,00 segundo de CPU durante uma amostra de 5 segundos (arredondado a centésimos). Ainda falta medir o impacto com captura e pontos verdes ativos durante o jogo; não há comparação quantitativa com a 1.0.5.

Na sessão ativa da 1.0.6, uma amostra de 10 segundos mediu 1,39 segundo de CPU no processo do aplicativo; outra amostra com menos atividade mediu 0,14 segundo em 10 segundos. O usuário relatou melhora parcial, mas ainda percebe travamentos. A máquina tinha uma conexão Ethernet ativa, enquanto a captura abria cinco adaptadores Npcap. A 1.0.7 prioriza o adaptador de rede com gateway para reduzir cópias e leitura de pacotes; se não encontrar correspondência, mantém a busca anterior. O efeito no jogo ainda depende de teste manual.

**Teste técnico da 1.0.7 (2026-11-06):** a seleção abriu 1 adaptador. Em 10 segundos com o cliente aberto, recebeu 109 pacotes UDP candidatos e decodificou 34 eventos, sem erros; não houve evento de recurso nem nova posição local nessa amostra. A projeção dos três recursos de teste continuou correta. O teste confirma que a captura não foi interrompida pela seleção do adaptador.

**Teste manual da 1.0.7 (2026-11-06):** o usuário relatou que o jogo ficou fluido com os pontos verdes ativos. Isso é uma observação dessa sessão, sem medição de latência de quadros. Uma captura de tela posterior mostrou que o círculo do overlay não acompanhava o formato de losango do minimapa; a versão 1.0.8 troca o contorno e adiciona ajuste de altura. Nenhuma posição de inimigo foi validada nessa imagem.

**Ajuste da 1.0.9 (2026-11-06):** nova captura de tela mostrou que o losango da 1.0.8 ainda era menor e estava à direita/abaixo do minimapa. Em uma tela de 1920×1080, o mapa ocupava aproximadamente 360×265 px, enquanto o overlay tinha 240×204 px. O novo posicionamento inicial usa 19% da largura útil, altura de 72% da largura, margem direita de 20 px e margem inferior de 35 px em relação à área útil do Windows. O botão de alinhamento reaplica esse cálculo. Isso é uma aproximação baseada na tela observada, não detecção automática dos limites do minimapa; requer conferência em jogo e ajuste manual para outras escalas/resoluções.

**Teste técnico da 1.0.10 (2026-11-06):** a compilação Release e a abertura passaram. Um teste com eventos no formato decodificado confirmou que entrada e saída de jogador atualizam o aviso e que um recurso esgotado por evento 46 desaparece. Uma captura passiva de 10 segundos abriu 1 adaptador, leu 183 pacotes UDP candidatos e 171 eventos sem erros; nessa amostra não houve novo recurso ou posição local. Ainda falta validar visualmente a borda laranja com uma entrada de jogador enquanto a captura está ativa.

O parser PhotonWire em `vendor/PhotonWire` veio de [AlbionPacketExplorer](https://github.com/LuluStudioX/AlbionPacketExplorer/tree/ff79e3f7a390dc393c70a8d8217fdafcccb64a99/libs/PhotonWire), licenciado sob MIT. A licença está preservada em `vendor/PhotonWire/LICENSE`. O código importado recebeu limites de tamanho para fragmentos e um ajuste de compilação.

## Teste manual

1. Abra o aplicativo; arraste o losango para alinhar com o minimapa.
2. Clique em **Alinhar ao minimapa** para restaurar a posição inicial. Ajuste largura, altura e escurecimento na janela de controle, e arraste se necessário.
3. Clique em **Iniciar captura** e mova o personagem. Confira os contadores de posição local e recursos.
4. Com **Mostrar recursos capturados** marcado, confira os pontos verdes. Desmarque para ocultá-los. Marque separadamente **Avisar sobre jogadores detectados (sem posição)** para testar a borda laranja quando chegar um evento de jogador.
5. Clique em **Travar posição**; o overlay deixa passar cliques para a janela de baixo. Destrave para arrastar novamente.
6. Feche a janela de controle; o overlay deve fechar junto.

O posicionamento é manual e não acompanha mudanças de tamanho, resolução ou posição da janela sob o overlay. Alguns modos exclusivos de tela cheia podem impedir sua exibição.

## Avaliação de captura passiva

O projeto público [AlbionOnline-StatisticsAnalysis](https://github.com/Triky313/AlbionOnline-StatisticsAnalysis) oferece captura alternativa por Npcap. Em [LibpcapPacketProvider.cs](https://github.com/Triky313/AlbionOnline-StatisticsAnalysis/blob/main/src/StatisticsAnalysisTool/Network/PacketProviders/LibpcapPacketProvider.cs), abre adaptadores, aplica filtro para UDP nas portas 5055/5056/5058, lê IPv4/IPv6 e repassa payloads ao receptor Photon. Não identifiquei nesse caminho um mecanismo de ocultação ou “bypass” de anticheat. Captura passiva não equivale a aprovação para um radar. O [README do projeto](https://github.com/Triky313/AlbionOnline-StatisticsAnalysis#is-this-allowed) diz que sua ferramenta não tem overlay e não rastreia jogadores fora da visão do usuário. Nossa captura Npcap alimenta os contadores e a exibição de recursos; nenhum marcador de jogador é desenhado sem posição validada.

## Regras e uso

Os [termos atuais do Albion Online, seção 13.3](https://albiononline.com/terms_and_conditions), proíbem ferramentas que deem vantagem sobre outros jogadores e mencionam software que intercepta ou captura dados do jogo. Um [comunicado oficial sobre software de terceiros](https://forum.albiononline.com/index.php/Thread/124819-Regarding-3rd-Party-Software-and-Network-Traffic-aka-do-not-cheat-Update-16-45-U/) cita expressamente radares e vantagens em tempo real como proibidos. Outro [comunicado oficial sobre banimentos](https://forum.albiononline.com/index.php/Thread/207759-Bans-Account-Security-Update-January-28/) informa que usar radar pode levar a banimento permanente. O uso de Npcap, um nome discreto ou a ausência de injeção não garantem conformidade nem impedem detecção. Para qualquer uso com o jogo, solicite avaliação escrita da Sandbox Interactive descrevendo exatamente o aplicativo e suas funções.

**Registro de documentação (2026-11-05):** revisão da captura Npcap e das regras oficiais; teste manual limitado a janela ou imagem de demonstração até haver autorização expressa para uso no jogo.

O código deste projeto foi escrito do zero; nenhum código do repositório GPL foi incorporado.
