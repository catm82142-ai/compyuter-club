import cors from "cors";
import express from "express";
import path from "path";
import QRCode from "qrcode";

import { store } from "./store";

export const app = express();
const publicDir = path.join(__dirname, "..", "public");
const pcPageFile = path.join(publicDir, "pc-page.html");
const kioskFile = path.join(publicDir, "kiosk.html");
const adminFile = path.join(publicDir, "admin.html");

app.use(cors());
app.use(express.json());
app.use(express.static(publicDir));

// Nginx/Caddy ortida ishlaganda QR ichida to'g'ri https manzil chiqishi uchun.
app.set("trust proxy", true);

app.get("/", (_req, res) => {
  res.json({
    message: "Computer Club MVP backend ishlayapti",
    docs: {
      health: "/health",
      pcPageHtml: "/p/PC-01",
      pcPageApi: "/api/pc/PC-01",
      adminPcs: "/admin/pcs",
    },
  });
});

app.get("/health", (_req, res) => {
  res.json({
    ok: true,
    service: "computer-club-mvp",
    now: new Date().toISOString(),
  });
});

app.get("/api/pc/:pcId", (req, res) => {
  const data = store.getPcPage(req.params.pcId);
  if (!data) {
    return res.status(404).json({ message: "PC topilmadi" });
  }

  return res.json(data);
});

app.get("/p/:pcId", (req, res) => {
  const data = store.getPcPage(req.params.pcId);
  if (!data) {
    return res.status(404).json({ message: "PC topilmadi" });
  }

  return res.sendFile(pcPageFile);
});

app.get("/api/qr", async (req, res) => {
  try {
    const pcId = String(req.query.pcId ?? "");
    const pc = store.getPc(pcId);

    if (!pc) {
      return res.status(404).json({ message: "PC topilmadi" });
    }

    const data = req.query.data
      ? String(req.query.data)
      : `${req.protocol}://${req.get("host")}/p/${pcId}`;

    const png = await QRCode.toBuffer(data, {
      type: "png",
      width: 600,
      margin: 1,
      errorCorrectionLevel: "H",
    });

    res.setHeader("Content-Type", "image/png");
    res.setHeader("Cache-Control", "public, max-age=86400");
    return res.send(png);
  } catch (error) {
    return res.status(500).json({
      message: error instanceof Error ? error.message : "QR yaratilmadi",
    });
  }
});

app.get("/api/kiosk/:pcId", (req, res) => {
  const data = store.getKioskState(req.params.pcId);
  if (!data) {
    return res.status(404).json({ message: "PC topilmadi" });
  }

  return res.json(data);
});

app.get("/k/:pcId", (_req, res) => {
  res.sendFile(kioskFile);
});

app.get("/admin", (_req, res) => {
  res.sendFile(adminFile);
});

// Ochiq serverda panelni himoyalash: ADMIN_KEY o'rnatilgan bo'lsa kalit talab qilinadi.
const adminKey = process.env.ADMIN_KEY;

function requireAdminKey(
  req: express.Request,
  res: express.Response,
  next: express.NextFunction,
) {
  if (!adminKey) {
    return next();
  }

  const provided = req.get("x-admin-key") ?? String(req.query.key ?? "");

  if (provided !== adminKey) {
    return res.status(401).json({ message: "Admin kaliti noto'g'ri" });
  }

  return next();
}

app.use("/admin", requireAdminKey);
app.use("/sessions", requireAdminKey);

app.post("/admin/pcs/:pcId/start", (req, res) => {
  try {
    const { minutes, phone, gameId } = req.body as {
      minutes?: number;
      phone?: string;
      gameId?: string;
    };

    if (!minutes || minutes <= 0) {
      return res.status(400).json({ message: "minutes majburiy" });
    }

    const session = store.startSessionDirect({
      pcId: req.params.pcId,
      minutes,
      phone,
      gameId,
    });

    return res.status(201).json(session);
  } catch (error) {
    return res.status(400).json({
      message: error instanceof Error ? error.message : "Noma'lum xato",
    });
  }
});

app.get("/admin/games", (_req, res) => {
  res.json(store.listGames());
});

app.get("/admin/pcs/:pcId/games", (req, res) => {
  res.json(store.listPcGames(req.params.pcId));
});

app.put("/admin/pcs/:pcId/games", (req, res) => {
  const { gameIds } = req.body as { gameIds?: string[] };

  if (!Array.isArray(gameIds)) {
    return res.status(400).json({ message: "gameIds massiv bo'lishi kerak" });
  }

  try {
    const pc = store.setPcGames(req.params.pcId, gameIds);
    return res.json(pc);
  } catch (error) {
    return res.status(400).json({
      message: error instanceof Error ? error.message : "Noma'lum xato",
    });
  }
});

app.post("/admin/pcs/:pcId/lock", (req, res) => {
  const { locked } = req.body as { locked?: boolean };

  if (typeof locked !== "boolean") {
    return res.status(400).json({ message: "locked true/false bo'lishi kerak" });
  }

  try {
    return res.json(store.setPcLock(req.params.pcId, locked));
  } catch (error) {
    return res.status(400).json({
      message: error instanceof Error ? error.message : "Noma'lum xato",
    });
  }
});

app.post("/cash-requests", (req, res) => {
  try {
    const { pcId, requestedMinutes, phone } = req.body as {
      pcId?: string;
      requestedMinutes?: number;
      phone?: string;
    };

    if (!pcId || !requestedMinutes) {
      return res.status(400).json({ message: "pcId va requestedMinutes majburiy" });
    }

    const result = store.createCashRequest({ pcId, requestedMinutes, phone });
    return res.status(201).json(result);
  } catch (error) {
    return res.status(400).json({
      message: error instanceof Error ? error.message : "Noma'lum xato",
    });
  }
});

app.get("/admin/pcs", (_req, res) => {
  res.json(store.listPcs());
});

app.get("/admin/sessions", (_req, res) => {
  res.json(store.listSessions());
});

app.get("/admin/cash-requests", (_req, res) => {
  res.json(store.listCashRequests());
});

app.post("/admin/cash-requests/:id/approve", (req, res) => {
  try {
    const { adminId, gameId } = req.body as { adminId?: string; gameId?: string };
    const result = store.approveCashRequest(req.params.id, adminId ?? "admin-1", gameId);
    return res.json(result);
  } catch (error) {
    return res.status(400).json({
      message: error instanceof Error ? error.message : "Noma'lum xato",
    });
  }
});

app.post("/admin/cash-requests/:id/reject", (req, res) => {
  try {
    const result = store.rejectCashRequest(req.params.id);
    return res.json(result);
  } catch (error) {
    return res.status(400).json({
      message: error instanceof Error ? error.message : "Noma'lum xato",
    });
  }
});

app.post("/agent/register", (req, res) => {
  try {
    const { pcId } = req.body as { pcId?: string };
    if (!pcId) {
      return res.status(400).json({ message: "pcId majburiy" });
    }

    const result = store.registerAgent(pcId);
    return res.status(201).json(result);
  } catch (error) {
    return res.status(400).json({
      message: error instanceof Error ? error.message : "Noma'lum xato",
    });
  }
});

app.post("/agent/heartbeat", (req, res) => {
  try {
    const { pcId } = req.body as { pcId?: string };
    if (!pcId) {
      return res.status(400).json({ message: "pcId majburiy" });
    }

    const result = store.heartbeat(pcId);
    return res.json(result);
  } catch (error) {
    return res.status(400).json({
      message: error instanceof Error ? error.message : "Noma'lum xato",
    });
  }
});

app.get("/agent/commands/:pcId", (req, res) => {
  res.json({
    pending: store.getPendingCommand(req.params.pcId),
    history: store.listCommandsForPc(req.params.pcId),
  });
});

app.post("/agent/commands/:commandId/ack", (req, res) => {
  try {
    const result = store.acknowledgeCommand(req.params.commandId);
    return res.json(result);
  } catch (error) {
    return res.status(400).json({
      message: error instanceof Error ? error.message : "Noma'lum xato",
    });
  }
});

app.post("/sessions/:sessionId/end", (req, res) => {
  try {
    const result = store.endSession(req.params.sessionId);
    return res.json(result);
  } catch (error) {
    return res.status(400).json({
      message: error instanceof Error ? error.message : "Noma'lum xato",
    });
  }
});
