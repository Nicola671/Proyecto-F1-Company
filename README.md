# 🏎️ F1 Career Manager

> Mobile F1 Management Game — Unity + C# — Pixel Art

## Estructura del Proyecto

```
Assets/_Project/
├── Scripts/         → Todo el código C#
│   ├── Core/        → Constantes, Enums, Managers globales
│   ├── Data/        → Modelos de datos (Piloto, Equipo, Circuito, etc.)
│   ├── AI/          → 6 sistemas de IA (Piloto, R&D, FIA, Narrador, Economía, Prensa)
│   ├── Simulation/  → Motor de simulación de carreras
│   ├── Market/      → Transferencias y negociaciones
│   ├── Regen/       → Generación de pilotos futuros
│   ├── Staff/       → Sistema de staff
│   ├── Events/      → Eventos aleatorios
│   ├── Legacy/      → Objetivos y Hall of Fame
│   ├── Injury/      → Sistema de lesiones
│   ├── UI/          → Pantallas y componentes de interfaz
│   └── Utils/       → Utilidades generales
├── Art/             → Sprites, UI, Animaciones (pixel art)
├── Audio/           → Música y efectos de sonido
├── Resources/Data/  → JSONs con datos del juego
├── Scenes/          → Escenas de Unity
└── Prefabs/         → Prefabs reutilizables
```

## Estado actual: Fase 1 — Fundamentos
- [x] Estructura de carpetas
- [x] Constantes y Enums
- [x] Modelos de datos (Piloto, Equipo, Circuito, Staff, Componente, Prensa, Contrato, Lesión)
- [x] JSON: 20 pilotos reales F1 2025
- [x] JSON: 10 equipos reales F1 2025
- [x] JSON: 24 circuitos reales F1 2025
- [ ] GameManager (core loop)
- [ ] Sistema de simulación de carrera
- [ ] IA de pilotos
- [ ] UI básica
