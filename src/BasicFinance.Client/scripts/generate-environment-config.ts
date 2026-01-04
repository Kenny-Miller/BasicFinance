import * as fs from 'node:fs';
import * as path from 'node:path';
import environmentConfigData from '../environment-config-data';

const distPath = path.join(
  process.cwd(),
  'dist/BasicFinance.Client/browser/environment-config.json',
);

try {
  fs.writeFileSync(distPath, JSON.stringify(environmentConfigData, null, 2));
  console.log(`✅ Config successfully written to: ${distPath}`);
} catch (error) {
  console.error('❌ Failed to write config file:', error);
  process.exit(1);
}
