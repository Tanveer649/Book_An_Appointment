# Hospital themes (colours)

The same build runs for every hospital. Only the **theme file** and one **config key** differ.

## Deploying for a hospital

1. **Theme file** — `wwwroot/themes/{name}.css` (lowercase; letters, numbers, `-`).
   For a new hospital copy `wwwroot/themes/aiims.css` and change the 4 colours:
   ```css
   :root {
     --primary: #8B1E3F;    /* buttons, links, selected slot, header title */
     --success: #1C9A6C;    /* verified, completed steps, booked */
     --accent: #D97706;     /* consultation-fee highlight */
     --footer-bg: #1A0F14;  /* footer background — keep it dark (footer text is light) */
   }
   ```
2. **Config** — `appsettings.json`:
   ```json
   "ThemeSettings": { "Theme": "aiims" }
   ```
3. **Publish** — keep only that hospital's file (and `default.css`) in `wwwroot/themes/` of the publish folder.

Changing `ThemeSettings:Theme` or a theme `.css` on a running server applies on the next page load — no rebuild.

## Ready-made themes (a – t)

`wwwroot/themes/` contains 20 tested palettes. For a new client pick one, rename it to `{client}.css`
and set `ThemeSettings:Theme` to `{client}`.

| File | Name | Primary | File | Name | Primary |
|---|---|---|---|---|---|
| a.css | Ocean Blue | `#1D4ED8` | k.css | Maroon | `#7F1D1D` |
| b.css | Emerald | `#047857` | l.css | Deep Cyan | `#0E7490` |
| c.css | Royal Purple | `#6D28D9` | m.css | Plum | `#6B21A8` |
| d.css | Crimson | `#B91C1C` | n.css | Olive | `#4D7C0F` |
| e.css | Golden Brown | `#A16207` | o.css | Rose | `#BE185D` |
| f.css | Indigo | `#4338CA` | p.css | Bronze | `#92400E` |
| g.css | Burnt Orange | `#C2410C` | q.css | Steel Grey | `#334155` |
| h.css | Magenta | `#A21CAF` | r.css | Midnight Navy | `#172554` |
| i.css | Forest Green | `#166534` | s.css | Berry | `#9D174D` |
| j.css | Denim | `#1E3A8A` | t.css | Sapphire | `#1E40AF` |

All pass WCAG AA contrast (≥ 4.5 : 1) for white button text, footer text / links, fee text and
"Verified" badges. When choosing your own `--primary`, keep it dark enough for white text
(check at e.g. webaim.org/resources/contrastchecker).

## How it works

| File | Role |
|---|---|
| `wwwroot/css/theme.css` | All colour variables. Brand defaults + every shade **derived** from the 4 brand colours (`color-mix`) + fixed design colours (white, text greys, red errors, gold rating) |
| `wwwroot/themes/default.css` | Original blue design with its exact shades. Used for `"default"`, an empty value, or a missing / invalid theme |
| `wwwroot/themes/{hospital}.css` | The hospital's 4 brand colours (may override any other variable from `theme.css` if a hospital wants exact shades) |
| `Helpers/ThemeProvider.cs` | Reads `ThemeSettings:Theme`, accepts only `[a-z0-9-]`, checks the file exists, falls back to `default` (warning logged once) |
| `Pages/Shared/_Layout.cshtml` | Loads `theme.css` → hospital theme → page CSS |

Derived shades (all overridable in a theme file):

| From | Variables |
|---|---|
| `--primary` | `--primary-dark`, `--primary-tint`, `--primary-soft`, `--primary-border`, `--primary-border-strong`, `--paper` (page background), `--surface-alt`, `--surface-soft`, `--line`, `--footer-link`, `--footer-text*` |
| `--success` | `--success-dark`, `--success-wash`, `--success-tint`, `--success-soft`, `--success-border`, `--success-border-strong` |
| `--accent` | `--accent-dark`, `--accent-tint`, `--accent-border` |
| `--footer-bg` | `--footer-bg-2` (gradient end) |

## Rules for developers

- **Never write a brand colour in page CSS, Razor `<style>` or inline styles** — use the variables
  (`var(--primary)`, `var(--primary-tint)`, …). Transparent variants:
  `color-mix(in srgb, var(--primary) 15%, transparent)`.
- Page CSS files must **not** redefine colour variables in their `:root` (fonts, radii, shadows are fine).
- Fixed colours (white, `--ink*` text, `--danger*`, `--gold*`, shadows, overlays) are the same for every hospital.
- Browser support: `color-mix` needs Chrome/Edge 111+, Safari 16.2+, Firefox 113+ (2023).
