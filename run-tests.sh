#!/usr/bin/env bash
# ============================================================
#  run-tests.sh — Local test runner for Scriptube Automation
#
#  Mirrors the GitHub Actions pipeline (quality gate → build →
#  test → Allure report) so the full suite can be driven from
#  a single command after cloning the repo and filling .env.
#
#  Usage:
#    ./run-tests.sh [OPTIONS]
#
#  Options:
#    --area      api | ui | webhook | all   (default: all)
#    --suite     smoke | regression | all   (default: smoke)
#    --threads   N                          (default: 1)
#    --skip-quality     Skip dotnet format + warnaserror gate
#    --install-playwright  (Re)install Playwright browser binaries
#    --no-report        Skip Allure report generation
#    --open-report      Open the HTML report in the browser
#    --help
#
#  Examples:
#    ./run-tests.sh --area api --suite smoke
#    ./run-tests.sh --area all --suite regression --threads 4
#    ./run-tests.sh --suite smoke --skip-quality --open-report
#    ./run-tests.sh --area webhook --suite smoke
# ============================================================

set -euo pipefail

# ─── colours ────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'
CYAN='\033[0;36m'; BOLD='\033[1m'; NC='\033[0m'

info()    { printf "${CYAN}[INFO]${NC}  %s\n" "$*"; }
ok()      { printf "${GREEN}[OK]${NC}    %s\n" "$*"; }
warn()    { printf "${YELLOW}[WARN]${NC}  %s\n" "$*"; }
err()     { printf "${RED}[ERROR]${NC} %s\n" "$*" >&2; }
step()    { printf "\n${BOLD}══ %s${NC}\n" "$*"; }

# ─── defaults ───────────────────────────────────────────────
AREA="all"
SUITE="smoke"
THREADS="1"
SKIP_QUALITY=false
INSTALL_PLAYWRIGHT=false
NO_REPORT=false
OPEN_REPORT=false

# ─── usage ──────────────────────────────────────────────────
usage() {
  cat <<EOF
${BOLD}Scriptube Test Runner${NC}

Usage: $(basename "$0") [OPTIONS]

  --area      api | ui | webhook | all   (default: all)
  --suite     smoke | regression | all   (default: smoke)
  --threads   N                          (default: 1)
  --skip-quality      Skip dotnet format + build --warnaserror gate
  --install-playwright  (Re)install Playwright browser binaries
  --no-report         Skip Allure report generation
  --open-report       Open the HTML report in the browser after generation
  --help              Show this help message

Examples:
  # Fast smoke across all areas (API + UI + Webhook)
  ./run-tests.sh

  # Only API smoke, skip format check
  ./run-tests.sh --area api --suite smoke --skip-quality

  # Full regression for API only, 4 parallel workers
  ./run-tests.sh --area api --suite regression --threads 4

  # All areas, all suites, open report when done
  ./run-tests.sh --suite all --threads 2 --open-report

  # After cloning — install browsers first
  ./run-tests.sh --install-playwright --suite smoke
EOF
  exit 0
}

# ─── argument parsing ────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --area)              AREA="$2";    shift 2 ;;
    --suite)             SUITE="$2";   shift 2 ;;
    --threads)           THREADS="$2"; shift 2 ;;
    --skip-quality)      SKIP_QUALITY=true;         shift ;;
    --install-playwright) INSTALL_PLAYWRIGHT=true;  shift ;;
    --no-report)         NO_REPORT=true;            shift ;;
    --open-report)       OPEN_REPORT=true;          shift ;;
    --help|-h)           usage ;;
    *)  err "Unknown option: $1"; echo; usage ;;
  esac
done

# ─── validate ────────────────────────────────────────────────
[[ "$AREA"    =~ ^(all|api|ui|webhook)$       ]] || { err "Invalid --area '$AREA'";    exit 1; }
[[ "$SUITE"   =~ ^(smoke|regression|all)$     ]] || { err "Invalid --suite '$SUITE'";  exit 1; }
[[ "$THREADS" =~ ^[1-9][0-9]*$               ]] || { err "--threads must be a positive integer"; exit 1; }

# ─── banner ──────────────────────────────────────────────────
printf "\n${BOLD}╔══════════════════════════════════════════╗${NC}\n"
printf   "${BOLD}║  Scriptube Automation — local run        ║${NC}\n"
printf   "${BOLD}╚══════════════════════════════════════════╝${NC}\n"
printf "  area    : %s\n" "$AREA"
printf "  suite   : %s\n" "$SUITE"
printf "  threads : %s\n" "$THREADS"
printf "  quality : %s\n" "$([[ "$SKIP_QUALITY" == "true" ]] && echo 'SKIPPED' || echo 'enabled')"
printf "\n"

# ─── move to repo root ───────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# ─── load .env ───────────────────────────────────────────────
step "Environment"
if [[ -f ".env" ]]; then
  set -a
  # shellcheck disable=SC1091
  source .env
  set +a
  ok ".env loaded"
else
  warn ".env not found — copy .env.example to .env and fill in your secrets"
  warn "Continuing with environment variables already set in the shell"
fi

# ─── quality gate ────────────────────────────────────────────
if [[ "$SKIP_QUALITY" == "false" ]]; then
  step "Quality Gate"
  info "dotnet format --verify-no-changes"
  dotnet format --verify-no-changes
  info "dotnet build --warnaserror"
  dotnet build --configuration Release --warnaserror --nologo -q
  ok "Quality gate passed"
else
  warn "Quality gate skipped (--skip-quality)"
fi

# ─── restore + build ─────────────────────────────────────────
step "Build"
info "dotnet restore"
dotnet restore --nologo -q
info "dotnet build (Release)"
dotnet build --configuration Release --no-restore --nologo -q
ok "Build succeeded"

# ─── playwright browser install ──────────────────────────────
PLAYWRIGHT_SCRIPT="tests/Scriptube.Automation.Tests/bin/Release/net10.0/playwright.ps1"
if [[ "$INSTALL_PLAYWRIGHT" == "true" ]]; then
  step "Playwright Browsers"
  if command -v pwsh &>/dev/null; then
    info "Installing Playwright browser binaries…"
    pwsh "$PLAYWRIGHT_SCRIPT" install chromium
    ok "Playwright browsers installed"
  else
    warn "pwsh not found — skipping Playwright install (browsers may already be cached)"
  fi
elif [[ "$AREA" == "ui" || "$AREA" == "all" ]]; then
  # Check silently; if browsers aren't installed tests will fail with a clear message.
  info "Tip: run with --install-playwright if UI tests fail on missing browser binaries"
fi

# ─── ngrok handling ──────────────────────────────────────────
NGROK_STARTED=false
NGROK_PID=""

needs_webhook() {
  [[ "$AREA" == "webhook" || "$AREA" == "all" ]]
}

if needs_webhook; then
  step "Webhook Receiver"

  if [[ -n "${WEBHOOK_RECEIVER_URL:-}" ]]; then
    ok "WEBHOOK_RECEIVER_URL is set — using external receiver, skipping ngrok"
  else
    RECEIVER_PORT="${WEBHOOK_RECEIVER_PORT:-5099}"

    if command -v ngrok &>/dev/null; then
      # Check if ngrok is already listening on the API port
      NGROK_API_PORT="${NGROK_API_PORT:-4040}"
      if curl -sf "http://localhost:${NGROK_API_PORT}/api/tunnels" &>/dev/null; then
        ok "ngrok is already running (API port ${NGROK_API_PORT})"
      else
        info "Starting ngrok tunnel on port ${RECEIVER_PORT}…"
        ngrok http "$RECEIVER_PORT" --log stdout > /tmp/scriptube-ngrok.log 2>&1 &
        NGROK_PID=$!
        NGROK_STARTED=true

        # Wait up to 10 s for the ngrok API to become available
        WAITED=0
        until curl -sf "http://localhost:${NGROK_API_PORT}/api/tunnels" &>/dev/null; do
          sleep 1
          WAITED=$((WAITED + 1))
          if [[ "$WAITED" -ge 10 ]]; then
            warn "ngrok did not start within 10 s — webhook tests requiring a live receiver will be skipped"
            break
          fi
        done

        if curl -sf "http://localhost:${NGROK_API_PORT}/api/tunnels" &>/dev/null; then
          TUNNEL_URL=$(curl -sf "http://localhost:${NGROK_API_PORT}/api/tunnels" \
            | grep -o '"public_url":"https:[^"]*"' | head -1 \
            | sed 's/"public_url":"//;s/"//')
          ok "ngrok tunnel active: ${TUNNEL_URL}"
        fi
      fi
    else
      warn "ngrok not found — webhook receiver tests will be skipped"
      warn "Install ngrok (https://ngrok.com/download) and run 'ngrok config add-authtoken <token>'"
    fi
  fi
fi

# Ensure ngrok is killed on exit (even on error)
cleanup() {
  if [[ "$NGROK_STARTED" == "true" && -n "$NGROK_PID" ]]; then
    info "Stopping ngrok (PID $NGROK_PID)…"
    kill "$NGROK_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT

# ─── clean previous allure results ───────────────────────────
ALLURE_RESULTS="tests/Scriptube.Automation.Tests/bin/Release/net10.0/allure-results"
ALLURE_REPORT="allure-report-local"

if [[ -d "$ALLURE_RESULTS" ]]; then
  info "Clearing previous allure-results…"
  rm -rf "$ALLURE_RESULTS"
fi

# ─── build NUnit filter ──────────────────────────────────────
step "Running Tests"

# Suite component
case "$SUITE" in
  smoke)      SUITE_FILTER="TestCategory=Smoke" ;;
  regression) SUITE_FILTER="TestCategory=Regression" ;;
  all)        SUITE_FILTER="" ;;
esac

# Area component
case "$AREA" in
  api)     AREA_FILTER="TestCategory=API" ;;
  ui)      AREA_FILTER="TestCategory=UI" ;;
  webhook) AREA_FILTER="TestCategory=Webhook" ;;
  all)     AREA_FILTER="" ;;
esac

# Combine with AND
if [[ -n "$SUITE_FILTER" && -n "$AREA_FILTER" ]]; then
  FILTER="${SUITE_FILTER}&${AREA_FILTER}"
elif [[ -n "$SUITE_FILTER" ]]; then
  FILTER="$SUITE_FILTER"
elif [[ -n "$AREA_FILTER" ]]; then
  FILTER="$AREA_FILTER"
else
  FILTER=""
fi

# Build dotnet test command
TEST_CMD="dotnet test tests/Scriptube.Automation.Tests \
  --configuration Release \
  --no-build \
  --logger \"trx;LogFileName=results.trx\" \
  --results-directory trx-results \
  -- NUnit.NumberOfTestWorkers=${THREADS}"

if [[ -n "$FILTER" ]]; then
  info "Filter: $FILTER"
  TEST_CMD="dotnet test tests/Scriptube.Automation.Tests \
  --configuration Release \
  --no-build \
  --filter \"$FILTER\" \
  --logger \"trx;LogFileName=results.trx\" \
  --results-directory trx-results \
  -- NUnit.NumberOfTestWorkers=${THREADS}"
else
  info "Filter: (none — running all tests)"
fi

# Run
TEST_EXIT=0
eval "$TEST_CMD" || TEST_EXIT=$?

if [[ "$TEST_EXIT" -eq 0 ]]; then
  ok "All tests passed"
else
  warn "Some tests failed (exit code $TEST_EXIT) — generating report anyway"
fi

# ─── Allure report generation ────────────────────────────────
if [[ "$NO_REPORT" == "false" ]]; then
  step "Allure Report"

  if ! command -v allure &>/dev/null; then
    warn "allure CLI not found — skipping report generation"
    warn "Install it: brew install allure  (macOS)  or  download from https://github.com/allure-framework/allure2/releases"
  else
    ALLURE_VERSION_FOUND=$(allure --version 2>/dev/null || echo "unknown")
    info "Allure CLI: $ALLURE_VERSION_FOUND"

    if [[ -d "$ALLURE_RESULTS" ]]; then
      info "Generating report → $ALLURE_REPORT"
      # Remove previous report output so we always get a clean HTML tree.
      rm -rf "$ALLURE_REPORT"
      # Compatible with both Allure 2.x (--clean) and 3.x (no --clean, manual rm above).
      allure generate --output "$ALLURE_REPORT" "$ALLURE_RESULTS" 2>/dev/null \
        || allure generate "$ALLURE_RESULTS" -o "$ALLURE_REPORT" --clean

      ok "Report ready: ${SCRIPT_DIR}/${ALLURE_REPORT}/index.html"

      if [[ "$OPEN_REPORT" == "true" ]]; then
        # Print the final summary now — allure open blocks, so the lines after it won't run.
        printf "\n"
        if [[ "$TEST_EXIT" -eq 0 ]]; then ok "Done ✓"; else err "Done with failures (exit $TEST_EXIT)"; fi
        info "Starting report server (Ctrl+C to stop)…"
        # allure open is the last operation — blocking intentionally.
        # Ctrl+C stops the server and exits the script cleanly.
        allure open "$ALLURE_REPORT"
      else
        info "Run with --open-report to serve and open automatically."
        info "Or serve manually with:  allure open $ALLURE_REPORT"
      fi
    else
      warn "No allure-results directory found at $ALLURE_RESULTS — nothing to report"
    fi
  fi
fi

# ─── exit with the test exit code ────────────────────────────
printf "\n"
if [[ "$TEST_EXIT" -eq 0 ]]; then
  ok "Done ✓"
else
  err "Done with failures (exit $TEST_EXIT)"
fi
exit "$TEST_EXIT"
