import { store } from "../src/store";

function check(label: string, actual: unknown, expected: unknown) {
  const ok = actual === expected;
  console.log(`${ok ? "PASS" : "FAIL"}  ${label}  (${actual}${ok ? "" : `, kutilgan ${expected}`})`);
  return ok;
}

store.startSessionDirect({ pcId: "PC-02", minutes: 10, phone: "+998901234567" });

const before = store.getKioskState("PC-02");
check("sessiya active", before?.activeSession?.status, "active");
check("PC busy", before?.runtimeStatus, "busy");

// Vaqt tugganini tasqlash: endsAt ni o'tmishga surish.
const session = (store as any).sessions.find((s: any) => s.pcId === "PC-02" && s.status === "active");
session.endsAt = new Date(Date.now() - 60_000).toISOString();

const after = store.getKioskState("PC-02");
check("muddati o'tgach sessiya ended", after?.activeSession ?? null, null);
check("PC yana available", after?.runtimeStatus, "available");

const lockCmd = (store as any).commands
  .filter((c: any) => c.pcId === "PC-02" && c.type === "lock")
  .pop();
check("lock buyrug'i yuborildi", lockCmd?.payload?.reason, "timeout");