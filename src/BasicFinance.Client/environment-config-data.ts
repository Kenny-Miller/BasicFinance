export default {
  basicFinanceApi:
    process.env['services__api__https__0'] || process.env['services__api__http__0'] || '',
  openIdAuthority: process.env['BASICFINANCE-GOOGLE-AUTHORITY'] || '',
  openIdClientId: process.env['BASICFINANCE-GOOGLE-CLIENTID'] || '',
};
