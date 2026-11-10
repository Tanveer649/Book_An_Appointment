# Deployment — publish, first install and updates (IIS)

How to publish the portal, install it on a client's IIS server, and update (redeploy) it later.
What to change in `appsettings.json` for a new hospital is in [Deploy-New-Client.md](Deploy-New-Client.md).

---

## 1. Publish

Visual Studio → right-click the project → **Publish** → Folder → **Release**
(or `dotnet publish "Book _An_Appointment.csproj" -c Release -o <folder>`).

The publish folder contains:

| Included | Not included |
|---|---|
| `Book _An_Appointment.dll` + library DLLs (PDFsharp, QRCoder, Newtonsoft) | **`App_Data`** (keys are created on each server — see section 4) |
| `wwwroot` (css, js, images, themes) | `Properties/launchSettings.json` (only for Visual Studio on your PC) |
| `Resources/Languages` (en / hi / bn) | source code (`.cs`) |
| `appsettings.json`, `web.config` | |
| `appsettings.Development.json`, `libman.json` (harmless on a server — may be deleted) | |

A published build always runs as **Production** — nothing to set.

---

## 2. First installation on a client server

1. Install the **.NET 10 Hosting Bundle** on the server (once per server).
2. Copy the publish folder to the site folder, e.g. `D:\Sites\BookAppointment`.
3. Edit `appsettings.json` for this client ([Deploy-New-Client.md](Deploy-New-Client.md)), put the client's logo / photos /
   theme file in `wwwroot`.
4. IIS → add website (or application) pointing to the folder; application pool **No Managed Code**; HTTPS binding.
5. Recommended: give the app-pool user (`IIS AppPool\<pool name>`) **write** permission on the site folder's `App_Data`
   (it is created on first start).
6. Open the portal. On first start it creates **`App_Data/keys`** by itself — nothing to copy or configure.
7. Check the log for `Hosting environment: Production` and `Data Protection keys folder: …`.

---

## 3. Updating (redeploying) the same client

**Before you start:** keep a copy of the client's current `appsettings.json` and their files in `wwwroot`
(logo, `images/HospitalImages`, `themes/<client>.css`) — a new publish contains the defaults from the project.

Any of these three ways works:

| Way | Steps | `App_Data` | Effect on patients |
|---|---|---|---|
| **A. Overwrite** (recommended) | Stop the site (or put `app_offline.htm` in the folder) → copy the new publish **over** the old folder → put back the client's `appsettings.json` / branding files if they were overwritten → start the site | stays — the publish has no `App_Data`, so nothing overwrites it | none |
| **B. Replace, keep App_Data** | Stop the site → delete everything **except `App_Data`** → copy the new publish → restore the client's `appsettings.json` / branding → start | stays | none |
| **C. Delete everything** | Stop the site → delete the **whole** folder including `App_Data` → copy the new publish → restore `appsettings.json` / branding → start | deleted → **created again automatically** on first start (new keys) | the portal works normally, **but** slip links / QR codes given to patients **before** the update show "invalid link", and anyone in the middle of booking at that moment has to start again |

> **Want to always do C without any effect?** Set `DataProtection:KeysPath` in `appsettings.json` to a folder **outside**
> the site folder, e.g. `D:\PortalKeys\<client>`. Deleting the site folder then never touches the keys.

### Visual Studio "Web Deploy" / folder publish option

If you publish straight to the server with Visual Studio, leave **"Remove additional files at destination" unticked**
— ticked, it deletes `App_Data` (same effect as way C).

---

## 4. App_Data — what it is and the rules

`App_Data/keys/key-….xml` are **ASP.NET Data Protection keys** (not API keys, not passwords). They sign / encrypt
slip links and their QR codes, the anti-forgery tokens and the session cookie.

| Question | Answer |
|---|---|
| Is it in the publish folder? | **No.** Each server creates its own on first start. |
| Do I change / copy keys per client? | **No.** Every client gets its own keys automatically. Never copy keys from one client to another. |
| Can I delete it? | Yes — the portal recreates it. Only old slip links / QR codes stop working (section 3, way C). |
| Server cannot write to it? | The portal still runs and logs *"Data Protection keys folder … is not writable"*; then slip links may stop working after an app-pool restart. Fix: give the app-pool user write permission on `App_Data`. |
| Several servers for one client (load balancer)? | Set `DataProtection:KeysPath` to **one shared folder** for all of them. |
| In git? | No — it is in `.gitignore`. |
| What is `App_Data/slips`? | "Find my slip" records (which slips were issued for which mobile — stored as a hash). Same rules as the keys: created automatically, not in the publish, keep it when updating. Deleting it only means older slips can no longer be found (their links still work). |

---

## 4a. Log files

The portal writes one log file per day to `logs/portal-yyyyMMdd.log` in the site folder (`FileLogging:Path`).
Only the last `FileLogging:RetainDays` days are kept (default **5** = today + 4 days before); older files are
deleted automatically at start-up and each time a new day starts.

- Like `App_Data`, the `logs` folder is **not** in the publish and is created on first start — give the app-pool user
  write permission on it (without it the portal still runs, just without log files).
- Patient mobile numbers are masked in the logs; API response bodies are only logged at Debug level.
- IIS's own `stdoutLogEnabled` in `web.config` is a different thing (start-up troubleshooting only) — leave it `false`;
  those files are never cleaned up.

## 5. After every deployment — quick check

- [ ] Portal opens: `https://<portal>/book` — hospital, specialities and doctors load
- [ ] Log: `Hosting environment: Production`, `Data Protection keys folder: …` (no "not writable" warning), no Critical messages
- [ ] Client branding correct (logo, name, theme colours, photos) — the right `appsettings.json` is in place
- [ ] Doctor link works: `https://<portal>/book/doctor/<doctorId>?facilityId=<id>`
- [ ] Wall poster QR (`https://<portal>/booking-qr`) opens the portal on a phone
- [ ] After a test booking: slip PDF downloads and its QR opens the slip

---

## 6. Rolling back

Keep the previous publish folder (e.g. rename it `BookAppointment_old`) until the new version is checked.
To roll back: stop the site, copy the old files back (keep `App_Data` and the client's `appsettings.json`), start the site.
