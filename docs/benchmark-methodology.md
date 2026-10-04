# ForgeFPS - Benchmark Methodology

## Overview

The benchmark system captures frame-level performance data using PresentMon (or compatible ETW-based tools), computes statistically meaningful metrics, and provides before/after comparison with significance testing.

## Data Source

### PresentMon
- **Source**: [GameTechDev/PresentMon](https://github.com/GameTechDev/PresentMon)
- **Method**: ETW (Event Tracing for Windows) — no injection, no overlay
- **Output**: CSV with per-frame timestamps, CPU/GPU times, late-frame flags
- **License**: MIT — redistributable

### Capture Process
1. User runs PresentMon manually (or via guided download):
   ```
   PresentMon.exe -process_name cs2 -output_file bench_before.csv -duration 60
   ```
2. User runs optimization in ForgeFPS
3. User repeats capture with same settings:
   ```
   PresentMon.exe -process_name cs2 -output_file bench_after.csv -duration 60
   ```
4. User imports both CSVs in ForgeFPS Benchmark page
5. ForgeFPS parses, computes metrics, shows comparison

## CSV Format

### Required Columns
| Column | Description |
|--------|-------------|
| `Frame#` | Sequential frame index (0-based) |
| `Timestamp_ms` | Wall-clock time in milliseconds since capture start |
| `FrameTime_ms` | Frame duration in milliseconds (1000/FPS) |
| `CPU_Time_ms` | CPU time spent on frame (optional) |
| `GPU_Time_ms` | GPU time spent on frame (optional) |
| `LateFrame` | Boolean: frame exceeded target duration (optional) |

### Decimal Separator Handling
- Auto-detected from first data row
- Supports `.` (Invariant/US) and `,` (pt-BR/European)
- Columns with mixed separators handled per-field

## Metric Definitions

### Average FPS
```
AverageFPS = TotalFrames / (DurationMs / 1000)
```

### Average Frame Time
```
AverageFrameTimeMs = Σ(FrameTimeMs) / TotalFrames
```

### 1% Low FPS (Industry Standard)
```
P99_FrameTime = 99th percentile of sorted FrameTimeMs (ascending)
OnePercentLowFPS = 1000 / P99_FrameTime
```
*Rationale*: 1% of frames have frametime ≥ P99. This represents the "worst 1%" experience.

### 0.1% Low FPS
```
P999_FrameTime = 99.9th percentile of sorted FrameTimeMs
PointOnePercentLowFPS = 1000 / P999_FrameTime
```

### Percentile Calculation (Nearest Rank Method)
```
Index = ceil(P * N) - 1
Clamped to [0, N-1]
```
Where P = 0.95/0.99/0.999, N = total frames

### Standard Deviation
```
StdDev = sqrt( Σ(FrameTime - Mean)² / N )
```

### Late Frame Percentage
```
LateFrame% = (Frames where LateFrame=true) / TotalFrames * 100
```

## Comparison Methodology

### Prerequisites for Valid Comparison
1. **Same game** (enforced by `GameName` match)
2. **Minimum 10 frames** per run (enforced)
3. **Same resolution/refresh rate** (user responsibility, documented in UI)
4. **Same map/scene** (user responsibility, documented in UI)

### Improvement Calculation
```
FPS_Improvement% = ((After.AvgFPS - Before.AvgFPS) / Before.AvgFPS) * 100
FrameTime_Improvement% = ((After.AvgFrameTime - Before.AvgFrameTime) / Before.AvgFrameTime) * 100
```
*Note: Negative FrameTime improvement = improvement (lower frametime = better)*

### Statistical Significance
- **Threshold**: |Δ| > 2%
- **Rationale**: Normal run-to-run variance in games is ~1-3%
- **Verdicts**:
  - `FPS_Improvement > 2%` → "Melhora significativa de FPS"
  - `FrameTime_Improvement < -2%` → "Melhora significativa de consistência"
  - Otherwise → "Variação dentro da margem de erro — resultado inconclusivo"

## Display in UI

### Metric Cards
| Metric | Color | Meaning |
|--------|-------|---------|
| Avg FPS | White | Overall performance |
| 1% Low | Green/Red | Consistency (green=good) |
| 0.1% Low | Green/Red | Extreme consistency |
| Avg FrameTime | White | Responsiveness |
| P95/P99 | White | Tail latency |
| StdDev | White | Stability |

### Comparison View
- **Green**: Significant improvement
- **Orange**: Inconclusive
- **Red**: Regression (if implemented)

## Limitations & Caveats

1. **Run-to-run variance**: Even identical runs show 1-3% variance. Multiple runs recommended.
2. **CPU/GPU bound**: FPS gain depends on bottleneck. CPU-bound games may not benefit from GPU optimizations.
3. **Thermal throttling**: Long runs may show artificial degradation.
4. **Background processes**: Other apps affect results. "Game Session" mode minimizes this.
5. **PresentMon version**: Different versions may have slightly different column layouts. Parser handles common variants.
6. **Not a substitute for rigorous testing**: This is a consumer-grade tool, not a scientific benchmark suite.

## References
- [PresentMon Documentation](https://presentmon.readthedocs.io/)
- [Frame Time Analysis](https://www.gamersnexus.net/guides/2574-frametime-benchmark-methodology)
- [1% Low FPS Explanation](https://www.techpowerup.com/gpu-specs/1-low-fps-explained)