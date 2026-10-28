import { store } from "../src/store";

function check(label: string, actual: unknown, expected: unknown) {
  const ok = actual === expected;
  console.log(`${ok ? "PASS" : "FAIL"}  ${label}  (${actual}${ok ? "" : `, kutilgan ${expected}`})`);
  return ok;
}

function tryStart(pcId: string, gameId: string) {
  try {
    store.startSessionDirect({ pcId, minutes: 60, gameId });
    return null;
  } catch (error) {
    return error instanceof Error ? error.message : "Noma'lum xato";
  }
}

function ackAll(pcId: string) {
  for (const command of (store as any).commands as Array<{ pcId: string; status: string }>) {
    if (command.pcId === pcId) {
      store.acknowledgeCommand((command as any).commandId);
    }
  }
}

// --- Ruxsat etilgan o'yinlar ---
const initial = store.listPcGames("PC-01").map((g) => g.gameId).sort();
check("PC-01 boshlang'ich ruxsat", initial.join(","), "cs2,dota2,steam");

store.setPcGames("PC-01", ["cs2", "dota2"]);
const saved = store.listPcGames("PC-01").map((g) => g.gameId).sort();
check("saqlangandan keyin", saved.join(","), "cs2,dota2");

// --- Ruxsat tekshiruvi ---
check("ruxsatsiz o'yin rad etildi", tryStart("PC-01", "gta5"), "Bu o'yin ushbu PC uchun ruxsat etilmagan");

const session = store.startSessionDirect({ pcId: "PC-01", minutes: 60, gameId: "cs2" });
check("ruxsatli o'yin bilan sessiya", session.gameId, "cs2");

// --- Bloklashni ochish ---
store.setPcLock("PC-01", false);
ackAll("PC-01");

const kiosk = store.getKioskState("PC-01");
check("kiosk bloklash holati", kiosk?.locked, false);
check("bloklashdan chiqarilganda sessiya faol", kiosk?.activeSession?.status, "active");

const unlockCmd = (store as any).commands.filter((c: any) => c.type === "setLock").pop();
check("setLock buyrug'i yuborildi", unlockCmd?.payload?.locked, false);

// --- Qayta bloklash ---
store.setPcLock("PC-01", true);
ackAll("PC-01");

const relockCmd = (store as any).commands.filter((c: any) => c.type === "setLock").pop();
check("qayta bloklash buyrug'i", relockCmd?.payload?.locked, true);
check("sessiya saqlandi", store.getKioskState("PC-01")?.activeSession?.status, "active");

// --- Noma'lum o'yin ---
try {
  store.setPcGames("PC-01", ["cs2", "noma_lum"]);
  check("noma'lum o'yin rad etildi", "rad etilmadi", "Noma'lum o'yin: noma_lum");
} catch (error) {
  check("noma'lum o'yin rad etildi", error instanceof Error ? error.message : "", "Noma'lum o'yin: noma_lum");
}
