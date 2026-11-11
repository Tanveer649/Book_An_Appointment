# Footer

The footer is short on purpose, so it also looks fine on phones and inside the mobile app.
Everything in it comes from `appsettings.json`, and **every part has its own on/off switch**.
View: `Pages/Footer.cshtml`.

```
Disclaimer: Book your appointment online and pay securely through our…   Read more
─────────────────────────────────────────────────────────────────────────────
About us   Contact us   Terms of Use   Privacy policy        [▶ GET IT ON Google Play] [ Download on the App Store]
© 2023 Akhil Systems Pvt. Ltd. All rights reserved.                       🔒 Secure patient portal
```

On a phone (≤ 640 px), the rows are centred and stacked. **Read more** opens the full disclaimer, and **Show less** closes it.

## Switches — `FooterSettings`

`true` shows the part and `false` removes it. A missing switch counts as `true`.
A link or badge appears only when its switch is `true` **and** its URL is set.

| Switch | Part | Its text / URL | Now |
|---|---|---|---|
| `Enabled` | **The whole footer** (`false` = no footer, e.g. inside the mobile app) | — | `true` |
| `ShowDisclaimer` | Disclaimer line | `HospitalSettings:DisclaimerText` | `true` |
| `DisclaimerCollapsed` | `true`: one line + "Read more"; `false`: full text always shown | — | `true` |
| `ShowPanelPhones` | "For Panel patients please call …" added to the disclaimer | `HospitalSettings:FooterPhone1-3` | `false` |
| `ShowAboutUs` | About us | `HospitalSettings:AboutUsUrl` | `true` |
| `ShowContactUs` | Contact us | `HospitalSettings:ContactUs` | `true` |
| `ShowTerms` | Terms of Use | `HospitalSettings:TermsUrl` | `true` |
| `ShowRefundPolicy` | Refund & Cancellations Policy | `HospitalSettings:RefundPolicyUrl` | `false` (no URL yet) |
| `ShowPrivacyPolicy` | Privacy policy | `HospitalSettings:PrivacyPolicyUrl` | `true` |
| `ShowGooglePlay` | Google Play badge | `FooterSettings:GooglePlayUrl` | `true` |
| `ShowAppStore` | App Store badge | `FooterSettings:AppStoreUrl` | `false` (no URL yet) |
| `ShowCopyright` | "© {year} {name}. All rights reserved." | `CopyrightYear` (empty = current year), `CopyrightName` | `true` |
| `ShowSecureBadge` | "🔒 Secure patient portal" | — | `true` |

If every part is switched off, the footer is not drawn at all, so no empty dark bar is left.

Examples:
- **Client does not want the disclaimer:** `"ShowDisclaimer": false`.
- **No footer inside the mobile app:** `"Enabled": false`.
- **Add a refund policy page:** set `HospitalSettings:RefundPolicyUrl`, then `"ShowRefundPolicy": true`.

How links open:
- A link to another site (`http…`) opens in a new tab, so the patient keeps the booking that is in progress.
- A link inside the portal (for example `/terms`) opens in the same tab.

The store badges are drawn by the portal (inline SVG), so there are no image files to copy.

## Translations — `Resources/Languages/{code}.json` → `Footer`

The keys are:
- `AboutUs`, `ContactUs`, `TermsOfUse`, `RefundPolicy`, `PrivacyPolicy`
- `Disclaimer`, `DisclaimerText`, `PanelPatients`
- `ReadMore`, `ReadLess`
- `GetItOn`, `DownloadOn`
- `Copyright` (`© {0} {1}. All rights reserved.`)
- `SecurePortal`, `Links`

`DisclaimerText` works like this:
- In English, the text from `HospitalSettings:DisclaimerText` is used.
- In other languages, the `Footer.DisclaimerText` translation is used. If you change the disclaimer for a client, update that key in `hi.json` and `bn.json` too.
