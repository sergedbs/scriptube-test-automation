# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.5.0] - 2026-02-25

### Added

#### Regression Test Suite — Full API Coverage (`Scriptube.Automation.Tests`)

- `SuccessVideoPathTests` — 10 tests, one per success-path video ID, each asserting `status == "completed"` and `transcript_text` non-empty:
  - `tstENAUT001` — English auto-captions
  - `tstKOONL001` — Korean with `translate_to_english`
  - `tstESAUT001` — Spanish with `translate_to_english`
  - `tstMULTI001` — multi-language video, English track selected
  - `tstYTTRN001` — French via YouTube auto-translate
  - `tstNOCAP001` — no captions, ElevenLabs AI fallback
  - `tstELABS001` — forced ElevenLabs transcription
  - `tstELTRN001` — ElevenLabs with German to English translation
  - `tstCACHE001` — cached YouTube transcript (cache-hit path)
  - `tstCACEL001` — cached ElevenLabs transcript (cache-hit path)
- `ErrorVideoPathTests` — 7 tests, one per error-path video ID, each asserting `status == "failed"` (live API uses `"failed"` at the item level, not `"error"`) and `error` field non-empty:
  - `tstPRIVT001` — private video
  - `tstDELET001` — deleted video
  - `tstAGERS001` — age-restricted video
  - `tstLONG0001` — video exceeding maximum duration
  - `tstRLIMT001` — rate-limited video
  - `tstTIMEO001` — connection timeout
  - `tstINVLD001` — malformed video data
- `PlaylistTests` — 4 tests covering all playlist compositions:
  - `PLtstOK00001` — all-success playlist, asserts exactly 3 items all `completed`
  - `PLtstMIX0001` — mixed playlist, asserts all items reach a terminal status and at least one is `completed`
  - `PLtstALL0001` — 5-video mixed playlist, asserts exactly 5 items all in terminal status
  - `PLtstERR0001` — all-error playlist, asserts batch reaches terminal status and all items are `failed` with non-empty error messages
- `RemainingEndpointTests` — 3 active tests + 1 ignored:
  - `GET /api/v1/usage` — HTTP 200, plan name present, all numeric quota fields non-negative
  - `GET /api/v1/transcripts/{batch_id}` with nil UUID — HTTP 404 (malformed string IDs return 422 from format validation; nil UUID bypasses validation and reaches the lookup)
  - `GET /api/v1/transcripts/{batch_id}` with foreign UUID — HTTP 404 (API hides resource existence from non-owners)
  - `GET /api/v1/credits/history` — implemented but `[Ignore]`d; endpoint not yet live on the public API

### Fixed

- `ErrorVideoPathTests` — initial item status assertion used `"error"`; corrected to `"failed"` after running against the live API revealed the actual status value returned
- `RemainingEndpointTests` — `GetBatch_NonExistentId_Returns404` initially used a free-form string ID which triggered HTTP 422 from format validation; replaced with the nil UUID (`00000000-0000-0000-0000-000000000000`) to bypass validation and reach the 404 path

## [0.4.0] - 2026-02-25

### Added

#### Regression Test Suite — Core Transcript Flows (`Scriptube.Automation.Tests`)

- `SubmitPollExportTests` — 5 active tests covering the full Submit → Poll → Export E2E flow:
  - Single English manual video (`tstENMAN001`) → poll to completion → transcript text non-empty
  - Batch of 3 success videos → all items reach `completed` status
  - Playlist URL (`PLtstOK00001`) → batch expands to ≥ 1 item, all items complete
  - Export in JSON format → response is valid JSON with `video_id` field present
  - Export in TXT format → non-empty plain-text response
  - SRT export test created but `[Ignore]`d — SRT is outside the supported format set (`json`, `csv`, `txt`)
- `CreditDeductionTests` — 3 active tests verifying credit balance changes after processing:
  - Balance before submit is a valid non-negative integer
  - Submit `tstENMAN001` → poll complete → balance decreases by exactly 4 credits
  - Submit `tstKOONL001` with `translate_to_english` → batch completes → deduction ≥ 4 credits
  - Precheck-matches-actual test created but `[Ignore]`d — `POST /api/v1/credits/precheck` returns HTTP 405
- `PrecheckEstimateTests` — full test bodies for precheck and estimate correlation; all `[Ignore]`d — both `POST /api/v1/credits/precheck` and `POST /api/v1/credits/estimate` return HTTP 405
- `BatchCancelTests` — full test bodies for cancel flow (submit → cancel immediately → verify `cancelled` status, cancel non-existent ID → 404, no credits charged); all `[Ignore]`d — `POST …/cancel` returns HTTP 405
- `RetryFailedTests` — full test bodies for retry-failed flow (submit error videos → items fail → retry → HTTP 2xx, retry non-existent batch → 404); all `[Ignore]`d — `POST …/retry-failed` returns HTTP 405
- `BatchLifecycleTests` — full test bodies for rerun and delete operations (rerun after completion, rerun non-existent → 404, delete → 200/204 → GET → 404); all `[Ignore]`d — both `POST …/rerun` and `DELETE /api/v1/transcripts/{batch_id}` return HTTP 405
- All test classes tagged `[Category("Regression")]`, `[Category("API")]`, `[AllureSuite("Regression")]`, and feature/tag attributes for NUnit filter and Allure report grouping
- Best-effort `[TearDown]` cleanup in every test class silently swallows 405 errors from `DeleteAsync` so test isolation is maintained even when the delete endpoint is unavailable

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
