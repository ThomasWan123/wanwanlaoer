# Apply UI upgrade patches (AndroidVer)

These patches upgrade **visuals only** (UI theming + richer map surface features). They do **not** change gameplay logic, stats, waves, or controls.

## What you’ll get

1. **Per-level UI theme switching** (based on `LEVELS[i].uiTheme`)
2. **UI color token system** (CSS variables) + per-theme overrides
3. **Richer procedural maps**: hills / greener plains / river+scorch, plus an improved **city gate** endpoint

## Files included

- `patch_01_level_theme_runtime.diff` (JS runtime theme switching)
- `patch_02_ui_tokens_and_themes.diff` (CSS tokens + theme overrides)
- `patch_03_map_biomes_artjs.diff` (Canvas map biomes in `art.js`)

## How to apply (recommended: git)

From the project root:

```bash
cd C:\GT-123\game\AndroidVer
git apply patch_01_level_theme_runtime.diff
git apply patch_02_ui_tokens_and_themes.diff
git apply patch_03_map_biomes_artjs.diff
```

If `git apply` is not available, open each `.diff` file and manually apply the changes to:

- `app/src/main/assets/www/js/main.js`
- `app/src/main/assets/www/js/ui.js`
- `app/src/main/assets/www/css/style.css`
- `app/src/main/assets/www/js/art.js`

