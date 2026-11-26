import { defineConfig, devices } from "@playwright/test";

const sql = process.env.ZOO_E2E_SQL;
if (!sql) throw new Error("Set ZOO_E2E_SQL to a dedicated, empty SQL Server test database.");

export default defineConfig({
  testDir: "./e2e",
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: "http://localhost:5173",
    trace: "retain-on-failure",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: [
    {
      command: "dotnet run --project ../backend/Zoo.Api/Zoo.Api.csproj -c Release --no-build --no-launch-profile",
      url: "http://localhost:5230/api/health",
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: "Development",
        ASPNETCORE_URLS: "http://localhost:5230",
        ConnectionStrings__DefaultConnection: sql,
        MediaStorageOptions__RootPath: "../../web/test-results/media",
      },
    },
    {
      command: "npm run dev -- --host localhost",
      url: "http://localhost:5173",
    },
  ],
});
