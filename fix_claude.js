const fs = require('fs');
const path = 'C:\\Users\\HP\\Rafi\\MyProject\\Farm-Beware\\CLAUDE.md';
let content = fs.readFileSync(path, 'utf8');

const trophyIdx = content.indexOf('Trophy mode stuck');
const kitchenIdx = content.indexOf('6. **Kitchen station not processing**: Check `stationInventory` assigned in Inspector, item has valid KitchenRecipe, `OnInventoryChanged` listener registered.');
const wallIdx = content.indexOf('7. **Wall occlusion not working**: Ensure walls have `WallOccluder` component, WallOcclusionManager.raycastMask includes wall layer.');

console.log('Trophy idx:', trophyIdx);
console.log('Kitchen idx:', kitchenIdx);
console.log('Wall idx:', wallIdx);

const trophyStr = content.substring(trophyIdx, trophyIdx + 110);
const kitchenStr = content.substring(kitchenIdx, kitchenIdx + 120);
const wallStr = content.substring(wallIdx, wallIdx + 100);

console.log('Trophy:', JSON.stringify(trophyStr));
console.log('Kitchen:', JSON.stringify(kitchenStr));
console.log('Wall:', JSON.stringify(wallStr));

const trophyOld = trophyStr;
const kitchenOld = kitchenStr;
const wallOld = wallStr;

const trophyNew = trophyStr + ' (Verified working in 2026-08-28 session)';
const kitchenNew = kitchenStr + ' (Verified working)';
const wallNew = wallStr + ' (Verified working)';

content = content.replace(trophyOld, trophyNew);
content = content.replace(kitchenOld, kitchenNew);
content = content.replace(wallOld, wallNew);

fs.writeFileSync('C:\\Users\\HP\\Rafi\\MyProject\\Farm-Beware\\CLAUDE.md', content, 'utf8');
console.log('Done');