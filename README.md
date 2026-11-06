# jogo4592radar 1.0.5

Base experimental para Windows 10/11, em C# e WPF. Mostra um círculo transparente com três pontos vermelhos **simulados**. A captura passiva por Npcap decodifica eventos Photon; uma prévia opcional coloca **pontos verdes de recursos** a partir de coordenadas recebidas, com escala e rotação ajustáveis. Posições de outros jogadores ainda não são exibidas. Não injeta DLL nem acessa memória do Albion Online.

## Histórico de versões

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

Para testar os recursos, inicie a captura **antes de entrar ou se mover** em uma área com recursos. Marque **Prévia de recursos reais (experimental)** para mostrar pontos verdes. O programa só desenha recursos depois de receber a posição local por uma requisição de movimento ou resposta de entrada no mapa. Ajuste **raio** e **rotação** observando o alinhamento; os valores iniciais (80 unidades e −135°) são provisórios. Pontos vermelhos continuam simulados. A prévia pode omitir recursos ou mostrar pontos antigos por até 60 segundos; não a trate como mapa validado.

Em um teste local de 12 segundos com o cliente aberto (2026-11-06), a captura abriu 5 adaptadores, observou 367 pacotes UDP candidatos, decodificou 580 eventos, incluindo 9 de entrada de jogador e 476 de movimento; 0 eventos de recurso apareceram nesse intervalo e houve 0 erros de decodificação. A origem de cada pacote não foi atribuída a um processo, e esses números não validam coordenadas nem a exibição no minimapa.

Em outra captura de 20 segundos com o personagem se movendo (2026-11-06), chegaram requisições de movimento com posição local e 33 eventos de recursos, incluindo lotes com coordenadas e recursos individuais. Um teste de projeção com valores dessa captura produziu três pontos verdes dentro do raio configurado. **Ainda falta comparar visualmente os pontos com o minimapa do jogo.** Uma captura posterior com o personagem parado não recebeu nova posição local, portanto a prévia ficou vazia como esperado.

O [OpenRadar](https://github.com/Nouuu/Albion-Online-OpenRadar/blob/main/web/scripts/drawings/PlayersDrawing.js) registra que o desenho de posições de jogadores está desativado porque essas posições chegam cifradas. Também observamos eventos de movimento com posições em bytes, sem coordenadas verificadas. Por isso, a presença de um jogador detectada no tráfego não é convertida em marcador vermelho.

O parser PhotonWire em `vendor/PhotonWire` veio de [AlbionPacketExplorer](https://github.com/LuluStudioX/AlbionPacketExplorer/tree/ff79e3f7a390dc393c70a8d8217fdafcccb64a99/libs/PhotonWire), licenciado sob MIT. A licença está preservada em `vendor/PhotonWire/LICENSE`. O código importado recebeu limites de tamanho para fragmentos e um ajuste de compilação.

## Teste manual

1. Abra o aplicativo sobre uma janela de teste ou imagem estática de minimapa; arraste o círculo para ajustar a posição. Não use este teste durante uma partida de Albion Online sem autorização expressa da Sandbox Interactive.
2. Ajuste o tamanho e o escurecimento na janela de controle.
3. Confira os três pontos vermelhos em movimento e o rótulo permanente **SIMULAÇÃO**.
4. Clique em **Pausar simulação**; os pontos devem desaparecer. Retome para exibi-los novamente.
5. Clique em **Travar posição**; o overlay deixa passar cliques para a janela de baixo. Destrave para arrastar novamente.
6. Feche a janela de controle; o overlay deve fechar junto.

O posicionamento é manual e não acompanha mudanças de tamanho, resolução ou posição da janela sob o overlay. Alguns modos exclusivos de tela cheia podem impedir sua exibição.

## Avaliação de captura passiva

O projeto público [AlbionOnline-StatisticsAnalysis](https://github.com/Triky313/AlbionOnline-StatisticsAnalysis) oferece captura alternativa por Npcap. Em [LibpcapPacketProvider.cs](https://github.com/Triky313/AlbionOnline-StatisticsAnalysis/blob/main/src/StatisticsAnalysisTool/Network/PacketProviders/LibpcapPacketProvider.cs), abre adaptadores, aplica filtro para UDP nas portas 5055/5056/5058, lê IPv4/IPv6 e repassa payloads ao receptor Photon. Não identifiquei nesse caminho um mecanismo de ocultação ou “bypass” de anticheat. Captura passiva não equivale a aprovação para um radar. O [README do projeto](https://github.com/Triky313/AlbionOnline-StatisticsAnalysis#is-this-allowed) diz que sua ferramenta não tem overlay e não rastreia jogadores fora da visão do usuário. Nossa captura Npcap alimenta os contadores e a prévia experimental de recursos; os marcadores vermelhos de jogadores permanecem simulados.

## Regras e uso

Os [termos atuais do Albion Online, seção 13.3](https://albiononline.com/terms_and_conditions), proíbem ferramentas que deem vantagem sobre outros jogadores e mencionam software que intercepta ou captura dados do jogo. Um [comunicado oficial sobre software de terceiros](https://forum.albiononline.com/index.php/Thread/124819-Regarding-3rd-Party-Software-and-Network-Traffic-aka-do-not-cheat-Update-16-45-U/) cita expressamente radares e vantagens em tempo real como proibidos. Outro [comunicado oficial sobre banimentos](https://forum.albiononline.com/index.php/Thread/207759-Bans-Account-Security-Update-January-28/) informa que usar radar pode levar a banimento permanente. O uso de Npcap, um nome discreto ou a ausência de injeção não garantem conformidade nem impedem detecção. Para qualquer uso com o jogo, solicite avaliação escrita da Sandbox Interactive descrevendo exatamente o aplicativo e suas funções.

**Registro de documentação (2026-11-05):** revisão da captura Npcap e das regras oficiais; teste manual limitado a janela ou imagem de demonstração até haver autorização expressa para uso no jogo.

O código deste projeto foi escrito do zero; nenhum código do repositório GPL foi incorporado.
