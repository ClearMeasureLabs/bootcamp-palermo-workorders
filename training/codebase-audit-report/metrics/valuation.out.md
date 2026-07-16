Blended base $120,000 x burden 1.5-1.75 / 260 = fully-loaded day rate $692-$808 (mid $750).

### Effort by productivity band (man-days = FP / FP-per-day; ~22 md/month)

| FP/man-day | Level | LOC/day equiv | System md | DevOps md | Test md |
|--:|---|--:|--:|--:|--:|
| 0.32 | Jones full-lifecycle baseline (recommended headline) | 24 | 484 | n/a (in System) | n/a (in System) |
| 0.50 | below baseline (large / high-ceremony) | 37 | 310 | 58 | 340 |
| 1.00 | coding-centric / well-run | 74 | 155 | 29 | 170 |
| 2.00 | high-performing small team | 149 | 78 | 15 | 85 |
| 3.00 | elite / best-in-class | 223 | 52 | 10 | 57 |

### Cost by band (man-days x fully-loaded day rate, mid $750)

| FP/man-day | Level | System $ | DevOps/Test $ | Total $ (coding-centric) |
|--:|---|--:|--:|--:|
| 0.32 | Jones full-lifecycle baseline (recommended headline) | $363k | n/a (in System) | n/a (in System) |
| 0.50 | below baseline (large / high-ceremony) | $232k | $298k | $531k |
| 1.00 | coding-centric / well-run | $116k | $149k | $266k |
| 2.00 | high-performing small team | $58k | $75k | $133k |
| 3.00 | elite / best-in-class | $39k | $50k | $88k |

### Anchors

- **Full-lifecycle replacement (headline, System only): $363k** (Jones 0.32 FP/day; includes design->test->docs->PM).
- Coding-centric best case (System, elite 3 FP/day): $39k.
- **Headline range across bands: $39k (elite coding) .. $363k (full-lifecycle).**

### Schedule sanity check

- calendar months ~ FP^0.34 = 155^0.34 = **5.6 months**
- full-lifecycle effort = 22.0 staff-months -> implied team = 22.0/5.6 = **4.0 people** (plausible).

Caveat: replacement/build-cost estimate, order-of-magnitude - NOT market value. Maintainability debt (see findings) makes CHANGING the code cost more per FP than a clean rebuild.
