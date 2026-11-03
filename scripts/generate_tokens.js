const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const secret = 'uqw9n0eu129nucouno132n21nu39c012uci2903u129ncu293ue32oru32nu3294u290c4u3294cnu39rcuwsidcbjewoibruewiu2398rnuewcnyewiugfnyw8rny398rcu39tu349ony3o8tfyaewo8wayorway8ocrawycruwayruiawbyowuybcraeworcbyawurbhyawuifywao8ryworhyaewuorawurwy8';
const issuer = 'favi.local';
const audience = 'favi.web';

function b64url(input) {
  return Buffer.from(input).toString('base64url');
}

const usersFile = path.resolve(__dirname, '../Favi-BE/Favi-BE.API/seed-output/users.csv');
const tokensFile = path.resolve(__dirname, '../Favi-BE/Favi-BE.API/seed-output/tokens.csv');

console.log(`Reading users from ${usersFile}...`);
const raw = fs.readFileSync(usersFile, 'utf8');
const lines = raw.split(/\r?\n/).filter(line => line.trim().length > 0);

const headerLine = lines[0];
const headers = headerLine.split(',');
const idIdx = headers.indexOf('profile_id');
const usernameIdx = headers.indexOf('username');
const roleIdx = headers.indexOf('role');

const headerObj = b64url(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
const now = Math.floor(Date.now() / 1000);
const exp = now + 90 * 86400; // 90 days validity for load tests

const outputLines = ['profile_id,username,token,generated_at'];
const isoNow = new Date().toISOString();

for (let i = 1; i < lines.length; i++) {
  const parts = lines[i].split(',');
  const profileId = parts[idIdx];
  const username = parts[usernameIdx];
  const role = (parts[roleIdx] || 'user').toLowerCase();

  const payloadObj = b64url(JSON.stringify({
    sub: profileId,
    jti: crypto.randomUUID(),
    unique_name: username,
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': profileId,
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': username,
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': role,
    type: 'access',
    nbf: now - 30,
    exp: exp,
    iss: issuer,
    aud: audience,
  }));

  const signature = crypto
    .createHmac('sha256', Buffer.from(secret, 'utf8'))
    .update(`${headerObj}.${payloadObj}`)
    .digest('base64url');

  const token = `${headerObj}.${payloadObj}.${signature}`;
  outputLines.push(`${profileId},${username},${token},${isoNow}`);
}

fs.writeFileSync(tokensFile, outputLines.join('\n'), 'utf8');
console.log(`Successfully generated ${outputLines.length - 1} valid tokens into ${tokensFile}`);

