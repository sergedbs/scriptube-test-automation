# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-02-26

### Added

#### Framework & Infrastructure

- Multi-project .NET 10 solution: `Core`, `Api`, `Ui`, `Webhooks` library projects with a single `Tests` entry point
- Layered configuration system — `appsettings.json` → environment-specific overrides → `.env` file → environment variables; strongly-typed `TestSettings` POCO with no hardcoded values
- `ApiClientBase` wrapping RestSharp with automatic `X-API-Key` injection, masked request/response logging via Serilog, and per-exchange Allure step attachment

#### API Tests

- **Smoke suite** — auth confirmation and fast health checks: credits balance, user info, plans list, 401 on missing/invalid key, 4xx on empty or non-YouTube URL submission
- **Regression suite** — full transcript lifecycle: Submit → Poll → Export for single videos, multi-video batches, and playlists; credit deduction verification; all 11 success-path and 7 error-path test video IDs; export format coverage (JSON, TXT); usage endpoint; 404 for unknown and foreign batch IDs

#### Webhook Tests

- In-process `HttpListener`-based webhook receiver with ngrok tunnel integration; graceful degradation with automatic test skip when ngrok is unavailable
- HMAC-SHA256 signature verification matching the API's canonical JSON signing scheme
- **Smoke suite** — webhook registration, SSRF rejection (localhost, RFC-1918 ranges), and available events listing
- **Regression suite** — full webhook lifecycle: register, get, list, delete, trigger test event, delivery log inspection, retry delivery, batch-completion delivery verification (with and without BYOK), and end-to-end HMAC signature assertion via the local receiver

#### UI Tests

- Page object models for all application screens with Allure step instrumentation on every action
- Session persistence via `AuthStateManager` — saves browser storage state after first login and reuses it across tests, with automatic stale-session re-authentication
- **Smoke suite** — credits balance display and pricing page plan rendering (unauthenticated)
- **Regression suite** — login flows (valid credentials, wrong password, blank email); batch submission and status polling; transcript preview; JSON and TXT export download; navigation header links

#### Reporting

- Allure NUnit integration with per-test step traces, HTTP exchange attachments, screenshot-on-failure, and `environment.properties` written on every run
- Allure report published to GitHub Pages after every CI run with history trend preserved across runs via `gh-pages` branch

#### CI/CD Pipeline (`.github/workflows/test.yml`)

- Four-job pipeline: `quality` → `smoke` → `regression` → `publish-report`; `publish-report` always runs regardless of test outcome
- `quality` job — `dotnet format --verify-no-changes` and `dotnet build --warnaserror` gate on every trigger
- `smoke` and `regression` jobs — Playwright browser install, ngrok tunnel setup, NUnit filter computed from `area` + `suite` inputs, configurable worker count via `threads` input
- `workflow_dispatch` inputs: `area` (api / ui / webhook / all), `suite` (smoke / regression / all), `threads`
- NuGet dependency caching and screenshot artifact upload on failure
- `run-tests.sh` — local pipeline script mirroring CI: quality gate, build, ngrok lifecycle, NUnit filter from `--area` / `--suite` / `--threads` flags, Allure report generation, and `--open-report` to serve the report over HTTP
