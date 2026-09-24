const { defineConfig } = require('@playwright/test');
const fs = require('node:fs');
const path = require('node:path');

const port = 5189;
const baseURL = `http://127.0.0.1:${port}`;
const edge = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';
const databaseDirectory = path.join(__dirname, 'tests', '.tmp');
fs.mkdirSync(databaseDirectory, { recursive: true });
const databasePath = path.join(databaseDirectory, `e2e-${process.pid}.db`);

module.exports = defineConfig({
  testDir: './tests/e2e',
  workers: 1,
  timeout: 30000,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL,
    browserName: 'chromium',
    launchOptions: !process.env.CI && fs.existsSync(edge) ? { executablePath: edge } : {},
    trace: 'retain-on-failure'
  },
  webServer: {
    command: `dotnet run --project src/MistoPodii/MistoPodii.csproj --urls ${baseURL}`,
    url: `${baseURL}/health`,
    reuseExistingServer: false,
    timeout: 120000,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      ConnectionStrings__DefaultConnection: `Data Source=${databasePath}`
    }
  }
});
