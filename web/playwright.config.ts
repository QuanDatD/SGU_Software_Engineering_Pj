import { defineConfig, devices } from "@playwright/test";

const sql = process.env.ZOO_E2E_SQL;
if (!sql) throw new Error("Set ZOO_E2E_SQL to a dedicated, empty SQL Server test database.");

export default defineConfig({
  testDir: "./e2e",
  workers: 1,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: "http://localhost:5173",
    timezoneId: "Asia/Ho_Chi_Minh",
    trace: "retain-on-failure",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: [
    {
      command: "dotnet run --project ../backend/Zoo.Tests.BrowserHost/Zoo.Tests.BrowserHost.csproj -c Release --no-build --no-launch-profile",
      url: "http://localhost:5230/api/health",
      timeout: 120_000,
      gracefulShutdown: { signal: "SIGTERM", timeout: 15_000 },
      env: {
        ZOO_E2E_SQL: sql,
      },
    },
    {
      command: "npm run dev -- --host localhost",
      url: "http://localhost:5173",
    },
  ],
});
