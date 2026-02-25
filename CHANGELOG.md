# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.3.0] - 2026-02-25

### Added

#### Smoke Test Suite — `Scriptube.Automation.Tests`

- `CreditsBalanceSmokeTests` — 3 tests: HTTP 200, non-negative numeric balance, non-empty plan name (`GET /api/v1/credits/balance`)
- `UserSmokeTests` — 4 tests: HTTP 200, email contains `@`, plan name present, user ID present (`GET /api/v1/user`)
- `PlansSmokeTests` — 2 tests: HTTP 200, non-empty plans list (`GET /api/v1/plans`)
- `AuthNegativeSmokeTests` — 3 tests: no API key → 401, invalid API key → 401 (balance + user endpoints)
- `ValidationNegativeSmokeTests` — 2 tests: empty URL list → 4xx, non-YouTube URL → 4xx (`POST /api/v1/transcripts`)
- `CreditCostsSmokeTests` — fixture created but `[Ignored]`; `GET /api/v1/credits/costs` is absent from the public OpenAPI spec
- `SeoToolSmokeTests` — fixture created but `[Ignored]`; `POST /tools/youtube-transcript` is absent from the public OpenAPI spec
- All test classes tagged `[Category("Smoke")]`, `[AllureSuite("Smoke")]`, and `[AllureFeature(...)]` / `[AllureTag(...)]` for NUnit filter and Allure report grouping

### Fixed

- `ConfigurationProvider` — replaced `WithEnvFiles(".env")` with `WithProbeForEnv(probeLevelsToSearch: 8)`; the previous approach resolved `.env` relative to `bin/Debug/net10.0/` at runtime so the repo-root `.env` file was never loaded
- `ValidationNegativeSmokeTests` — empty-URL-list test now constructs `TranscriptRequest { Urls = [] }` directly instead of going through `TranscriptRequestBuilder.Build()`, which intentionally guards against empty lists and was throwing `InvalidOperationException` before the request reached the server

## [0.2.0] - 2026-02-24

### Added

#### Request / Response Models — `Scriptube.Automation.Api`

- `TranscriptRequest` — `urls[]`, `use_byok`, `translate_to_english` (matches OpenAPI schema exactly)
- `TranscriptSubmitResponse` — `batch_id`, `batch_number`, `status`, `url_count`, `message`, `key_source`
- `BatchStatusResponse` — `batch_id`, `batch_number`, `status`, `created_at`, `completed_at`, `items[]`
- `TranscriptItemResponse` — `video_id`, `url`, `title`, `channel`, `status`, `transcript_text`, `transcript_language`, `duration_seconds`, `error`
- `CreditBalanceResponse` — `credits_balance`, `plan`, `daily_used`, `daily_limit`
- `PrecheckRequest` / `PrecheckResponse` + `PrecheckItemResponse` — URL pre-validation with per-item estimated cost
- `EstimateRequest` / `EstimateResponse` + `EstimateItemResponse` — video-ID cost estimation
- `UsageResponse` — `plan`, `daily_used`, `daily_limit`, `daily_remaining`, `monthly_used`, `total_processed`
- `PlansListResponse` + `PlanInfoResponse` — full plan catalogue with pricing, limits, and feature list
- `UserInfoResponse` — `user_id`, `email`, `plan`, `email_verified`, `credits_balance`, `total_videos_processed`, `created_at`
- `HealthResponse` — `status`, `version`
- `CreditCostsResponse` — cost table map
- `CreditHistoryResponse` + `CreditTransactionResponse` — paginated transaction log
- `WebhookResponse` — full webhook detail model
- `WebhookStatusResponse` — status envelope for register / delete / retry operations
- `WebhookListResponse` — paginated list of webhooks
- `AvailableEventsResponse` — event names + descriptions map
- `DeliveryLogsResponse` + `DeliveryLogResponse` — paginated webhook delivery history
- `TestEventResponse` — test-fire result with delivery ID and response code
- `HttpValidationError` + `ValidationError` — exact mapping of the OpenAPI `HTTPValidationError` / `ValidationError` 422 schemas (`loc` typed as `List<JsonElement>` to handle mixed string/integer path segments)
- `ApiErrorResponse` — generic 4xx/5xx error envelope (`detail`, `message`, `code`)
- `WebhookRegisterRequest` — `url` (≤ 2048 chars), `events[]` (≥ 1 item), `secret` (16–256 chars)

#### Builder Pattern — `Scriptube.Automation.Api`

- `TranscriptRequestBuilder` — fluent builder with `.WithUrl()`, `.WithUrls()`, `.WithPlaylist()`, `.WithTranslation()`, `.WithByok()`, `.Build()`; guards against empty URL lists
- `PrecheckRequestBuilder` — fluent builder with `.WithUrl()`, `.WithUrls()`, `.Build()`; guards against empty URL lists

#### API Service Clients — `Scriptube.Automation.Api`

- `TranscriptsClient` — `SubmitAsync`, `GetBatchAsync`, `ListBatchesAsync`, `PollUntilCompleteAsync`, `ExportAsync`, `CancelAsync`, `RetryFailedAsync`, `RerunAsync`, `DeleteAsync`; `PollUntilCompleteAsync` reads poll interval and timeout from `TestSettings.Timeouts` with no hardcoded values; terminates on `completed`, `failed`, or `cancelled`
- `CreditsClient` — `GetBalanceAsync`, `PrecheckAsync`, `EstimateAsync`, `GetCostsAsync`, `GetHistoryAsync`
- `UserClient` — `GetUserAsync`
- `UsageClient` — `GetUsageAsync`
- `PlansClient` — `GetPlansAsync`
- `SeoToolClient` — `GetTranscriptAsync`; constructed with `requiresAuth: false` — no `X-API-Key` header injected

#### Test Data Constants — `Scriptube.Automation.Api`

- `VideoIds` — 18 `tst*` video ID constants (11 success paths, 7 error paths) with corresponding pre-built `*Url` fields and a `ToUrl(videoId)` helper
- `PlaylistUrls` — 4 `PLtst*` playlist URL constants (`AllSuccess`, `Mixed`, `AllMixed`, `AllErrors`)
- `ExportFormats` — `Json`, `Txt`, `Srt` string constants matching the export endpoint's `format` query parameter

## [0.1.0] - 2026-02-24

### Added

#### Project Structure

- Solution with five projects: `Core`, `Api`, `Ui`, `Webhooks` (under `src/`) and `Tests` (under `tests/`)
- Project references wired: `Tests → Api → Core`, `Tests → Ui → Core`, `Tests → Webhooks → Core`
- `TreatWarningsAsErrors` enabled on all `src/` projects to enforce a clean build

#### Configuration System (`Scriptube.Automation.Core`)

- `TestSettings` — strongly-typed POCO covering `BaseUrl`, `ApiKey`, `Credentials`, `Timeouts`, `Retry`, `Browser`, and `WebhookReceiverUrl`
- `ConfigurationProvider` — layered config loader: `appsettings.json` → `appsettings.{env}.json` → `.env` file → environment variables (highest priority wins)
- `appsettings.json` — committed default schema with safe placeholder values; copied to output directory on build
- `.env.example` — template for local developer secrets (never committed)
- `dotenv.net` integration for automatic `.env` file loading in local runs

#### HTTP Infrastructure (`Scriptube.Automation.Core`)

- `LoggingHttpHandler` — `DelegatingHandler` that captures every request/response exchange; masks `X-API-Key` header value in all log output; fires `OnExchange` event for downstream consumers
- `HttpExchange` record — immutable snapshot of a single HTTP round-trip (method, URL, headers, body, status code, elapsed ms)
- `ApiClientBase` — `RestClient` wrapper that injects `X-API-Key` on every request, pipes traffic through `LoggingHttpHandler`, and exposes `ExecuteAsync<T>` / `ExecuteAsync`

#### Allure Reporting (`Scriptube.Automation.Core`)

- `AllureRestLogger` — subscribes to `ApiClientBase.OnExchange` and attaches each HTTP exchange as an Allure step with a formatted `text/plain` attachment (request headers + body, response status + body, elapsed time)

#### Base Test Fixtures (`Scriptube.Automation.Core`)

- `GlobalSetupFixture` — NUnit `[SetUpFixture]` that initialises Serilog structured console logging once per assembly run
- `BaseTest` — abstract base class that loads `TestSettings` in `[SetUp]` and exposes it to all derived test classes

#### API Test Base (`Scriptube.Automation.Api`)

- `BaseApiTest : BaseTest` — creates an authenticated `ApiClientBase`, attaches `AllureRestLogger`, and disposes the client in `[TearDown]`

#### UI Test Base (`Scriptube.Automation.Ui`)

- `BaseUiTest : BaseTest` — launches a headless Chromium browser via Playwright per test; each test receives an isolated `IBrowserContext` and `IPage`; takes a full-page screenshot on failure and attaches it to the Allure report; cleans up browser resources in teardown
- `Allure.Net.Commons` added to the `Ui` project for screenshot attachment support

#### Webhook Test Base (`Scriptube.Automation.Webhooks`)

- `BaseWebhookTest : BaseTest` — provides an authenticated `ApiClientBase` with Allure logging; placeholder for HMAC verifier and receiver helpers (Iteration 6)

#### Repository Hygiene

- `.gitignore` — extended with automation-specific rules: `allure-results/`, `allure-report/`, `playwright-screenshots/`, `playwright/.cache/`, `appsettings.local.json`, `appsettings.*.local.json`
- `allureConfig.json` — Allure output directory and report title configured for the test project
- `README.md` — project overview, tech stack table, prerequisites, local setup instructions, environment variable reference, CI usage examples, and project structure map

### Changed

- Removed placeholder `Class1.cs` from all `src/` projects and `UnitTest1.cs` from the test project

[Unreleased]: https://github.com/sergedbs/scriptube-test-automation/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/sergedbs/scriptube-test-automation/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/sergedbs/scriptube-test-automation/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/sergedbs/scriptube-test-automation/compare/initial...v0.1.0
