# Changelog

Todas as mudanças notáveis deste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-10-04

### Adicionado

**Fase 1 — Fundação**
- Solução ForgeFPS.sln com 10 projetos de código e 4 de testes
- Clean Architecture: Domain, Application, Infrastructure, Presentation
- Contratos `IOptimizationAction`, `ActionSnapshot`, `IApplyContext`
- Janela WinUI 3 com navegação (Início, Jogos, CS2, Windows, Benchmark, Histórico, Diagnóstico)
- Injeção de dependência com `Microsoft.Extensions.DependencyInjection`

**Fase 2 — Motor Seguro**
- `OptimizationTransactionEngine` com rollback em ordem inversa
- Ações reais: plano de energia (powercfg), Game Mode (registro HKCU), preferência de GPU (HKCU)
- `PathValidator` — prevenção de path traversal e symlinks
- `AtomicWriter` — escrita atômica (temp → flush → replace)
- `SnapshotService` — captura/restauração de registro e arquivos com tipos (DWord, QWord, String, Binary)
- Modo simulação (detect + preview sem modificar nada)

**Fase 3 — Integração Windows**
- `OptimizationSessionService` — orquestra Inspect/Simulate/ApplyWithConsent
- `HistoryStore` — registro de sessões com estado por ação
- Página Windows com cards interativos, consentimento explícito e diálogos de resultado
- Página Histórico com timeline de sessões

**Fase 4 — Módulo CS2**
- `Cs2SteamDiscovery` — localiza bibliotecas Steam via libraryfolders.vdf e appmanifest_730.acf
- `Cs2ConfigParser` — parser de client.cfg que preserva comentários, binds e chaves desconhecidas (merge semântico)
- Perfis Seguro e Competitivo pré-definidos
- `Cs2ConfigApplyAction` — aplica perfil com backup SHA-256 e rollback
- `Cs2LaunchOptionsAction` — apenas opções seguras (-novid, -fps_max); nunca -high, -threads ou -allow_third_party_software

**Fase 5 — Benchmark**
- `PresentMonCsvParser` — parser CSV com auto-detecção de separador decimal (`,` vs `.`)
- Métricas: FPS médio, 1% low (P99), 0.1% low (P99.9), P95, desvio padrão
- `BenchmarkRunner` — importa CSVs, compara Antes/Depois com veredito e significância (>2%)
- 50 testes automatizados passando (xUnit + FluentAssertions + Moq)

**Fase 6 — Acabamento**
- Páginas funcionais: Home (Analisar meu PC), Jogos, CS2, Benchmark (importar Antes/Depois), Diagnóstico (matriz de compatibilidade)
- CI GitHub Actions (restore, build, test, artifacts)
- Documentação completa: architecture.md, safety-model.md, optimization-catalog.md, benchmark-methodology.md, build-and-release.md, troubleshooting.md
- Directory.Build.props com versionamento centralizado
- README em português

### Segurança
- Nenhuma injeção de DLL, manipulação de memória ou bypass de VAC/Trusted Mode
- Allowlist de ações — nenhum comando arbitrário executável
- Consentimento explícito antes de qualquer modificação
- Snapshots antes de cada alteração com rollback por item ou sessão
- Telemetria desativada por padrão

## [Unreleased]

### Planejado
- Persistência SQLite (HistoryStore, BenchmarkRuns)
- MSIX assinado para Microsoft Store
- Auto-update com verificação de assinatura
- Módulos para outros jogos (Dota 2, Valorant)
