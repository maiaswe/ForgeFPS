# ForgeFPS - Otimizador de Games para Windows e CS2

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Tests](https://img.shields.io/badge/tests-50%20passing-brightgreen.svg)](./CHANGELOG.md)

**ForgeFPS** é um otimizador mensurável e reversível para Windows e Counter-Strike 2. Diferente de "tweaks mágicos", o ForgeFPS segue um fluxo transparente:

```
Diagnosticar → Criar backup → Medir → Aplicar → Medir novamente → Restaurar se necessário
```

## Status do MVP (v0.1.0)

| Componente | Status |
|------------|--------|
| Solução (10 projetos src + 4 testes) | ✅ Completo |
| Testes automatizados | ✅ 50 passando |
| Ações Windows (energia, Game Mode, GPU) | ✅ 4 implementadas |
| Ações CS2 (config, launch options) | ✅ 2 implementadas |
| Motor de transações com rollback | ✅ Completo |
| Benchmark (PresentMon CSV) | ✅ Completo |
| UI WinUI 3 (7 páginas) | ✅ Completo |
| Documentação (docs/) | ✅ 6 arquivos |
| CI GitHub Actions | ✅ Configurado |
| Persistência SQLite | 🔜 Planejada |
| Instalador MSIX assinado | 🔜 Planejado |

> **Nota sobre build:** o projeto `ForgeFPS.App` (WinUI 3) requer o compilador XAML que roda apenas com as ferramentas do Visual Studio / Windows App SDK instaladas. Os demais 13 projetos compilam via CLI com `dotnet build`. Abra a solução no Visual Studio 2022 para compilar o app completo.

## 🛡️ Segurança

O aplicativo **NÃO**:
- Injeta DLLs em jogos
- Lê/escreve memória do jogo
- Burla VAC/Trusted Mode
- Desativa segurança do Windows
- Executa comandos arbitrários

Todas as ações administrativas seguem uma allowlist e são idempotentes.

## 📦 Requisitos

- Windows 10 (build 1809+) ou Windows 11
- .NET 8 SDK
- Visual Studio Build Tools (para compilação)

## 🔧 Como Compilar

```bash
# Clonar repositório
git clone https://github.com/ForgeFPS/forgefps.git
cd forgefps

# Restaurar pacotes
dotnet restore ForgeFPS.sln

# Compilar em Release x64
dotnet build -c Release -p:Platform=x64

# Executar testes
dotnet test

# Executar aplicação
dotnet run --project src/ForgeFPS.App/ForgeFPS.App.csproj -c Release
```

## 🧪 Testes

```bash
# Todos os testes
dotnet test

# Testes com cobertura
dotnet test /p:CollectCoverage=true
```

## 📁 Estrutura do Projeto

```
ForgeFPS/
├── src/
│   ├── ForgeFPS.App/              # WinUI 3, Views, ViewModels
│   ├── ForgeFPS.Domain/           # Entidades, enums, interfaces
│   ├── ForgeFPS.Application/      # Casos de uso, motor de transações
│   ├── ForgeFPS.Infrastructure.Windows/  # Registro, powercfg, hardware
│   ├── ForgeFPS.ElevatedHost/     # Helper mínimo executado com elevação
│   ├── ForgeFPS.Games.Abstractions/  # Contratos para módulos de jogos
│   ├── ForgeFPS.Games.CS2/        # Detecção e otimizações do CS2
│   ├── ForgeFPS.Benchmarking/     # Captura e comparação de desempenho
│   ├── ForgeFPS.Contracts/        # DTOs IPC e schemas versionados
│   └── ForgeFPS.Shared/           # Utilitários compartilhados
├── tests/
│   ├── ForgeFPS.Domain.Tests/
│   ├── ForgeFPS.Application.Tests/
│   └── ForgeFPS.Benchmarking.Tests/
└── docs/
    ├── architecture.md
    ├── safety-model.md
    └── optimization-catalog.md
```

## 📝 Roadmap

- [x] Fundação da solução (Fase 1)
- [ ] Motor seguro com transações (Fase 2)
- [ ] Ações Windows (energia, Game Mode, GPU) (Fase 3)
- [ ] Módulo CS2 completo (Fase 4)
- [ ] Integração PresentMon (Fase 5)
- [ ] Acabamento e documentação (Fase 6)

## 📄 Licença

MIT License - veja [LICENSE](LICENSE) para detalhes.

## 🤝 Contribuindo

1. Fork o repositório
2. Crie uma branch (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 🙏 Agradecimentos

- Microsoft Windows App SDK
- PresentMon por medições de performance
- Comunidade CS2 para feedback sobre Trusted Mode