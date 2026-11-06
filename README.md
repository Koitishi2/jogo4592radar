# jogo4592radar 1.0.1

Base experimental para Windows 10/11, em C# e WPF. Mostra um círculo transparente sobre o minimapa com três pontos vermelhos **simulados**. Não detecta jogadores reais, não lê pacotes, não injeta DLL e não acessa memória do Albion Online.

## Histórico de versões

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

## Teste manual

1. Abra o aplicativo e arraste o círculo para cima do minimapa do jogo, com o jogo em janela ou tela cheia sem exclusividade.
2. Ajuste o tamanho e o escurecimento na janela de controle.
3. Confira os três pontos vermelhos em movimento e o rótulo permanente **SIMULAÇÃO**.
4. Clique em **Pausar simulação**; os pontos devem desaparecer. Retome para exibi-los novamente.
5. Clique em **Travar posição**; o overlay deixa passar cliques para a janela de baixo. Destrave para arrastar novamente.
6. Feche a janela de controle; o overlay deve fechar junto.

O posicionamento é manual e não acompanha mudanças de tamanho, resolução ou posição da janela do jogo. Alguns modos exclusivos de tela cheia podem impedir a exibição de overlays.

## Avaliação de captura passiva

O projeto público [AlbionOnline-StatisticsAnalysis](https://github.com/Triky313/AlbionOnline-StatisticsAnalysis) oferece captura alternativa por Npcap. Em `LibpcapPacketProvider.cs`, abre adaptadores, aplica filtro para UDP nas portas 5055/5056/5058, lê IPv4/IPv6 e repassa payloads ao receptor Photon. Isso demonstra uma arquitetura de captura **passiva**, mas não comprova que dados suficientes para posições confiáveis de inimigos estão disponíveis, nem que o uso de overlay seja permitido pelas regras atuais do jogo. O próprio README do projeto descreve sua ferramenta como sem overlay e limitada a jogadores visíveis. Nesta versão, a captura Npcap foi deliberadamente deixada fora do aplicativo. Qualquer integração futura exige validação técnica e revisão das regras atuais.

O código deste projeto foi escrito do zero; nenhum código do repositório GPL foi incorporado.
