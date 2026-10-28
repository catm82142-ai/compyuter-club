export type PcRuntimeStatus = "available" | "busy" | "offline";
export type SessionStatus =
  | "pending_payment"
  | "paid"
  | "active"
  | "ended"
  | "canceled";
export type PaymentStatus = "created" | "paid" | "failed" | "canceled";
export type CashRequestStatus = "pending" | "approved" | "rejected" | "expired";
export type AgentCommandType = "startSession" | "stopSession" | "lock" | "setLock";

export interface Pc {
  pcId: string;
  name: string;
  runtimeStatus: PcRuntimeStatus;
  kioskCode: string;
  lastHeartbeatAt: string | null;
  agentRegistered: boolean;
  locked: boolean;
  allowedGameIds: string[];
  createdAt: string;
  updatedAt: string;
}

export interface Game {
  gameId: string;
  name: string;
}

export interface Session {
  sessionId: string;
  pcId: string;
  userPhone?: string;
  gameId?: string;
  status: SessionStatus;
  durationMinutes: number;
  price: number;
  startsAt: string | null;
  endsAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface Payment {
  paymentId: string;
  provider: "cash" | "payme" | "click";
  sessionId: string;
  status: PaymentStatus;
  amount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CashRequest {
  requestId: string;
  pcId: string;
  phone?: string;
  requestedMinutes: number;
  status: CashRequestStatus;
  approvedByAdminId?: string;
  createdAt: string;
  updatedAt: string;
}

export interface AgentCommand {
  commandId: string;
  pcId: string;
  type: AgentCommandType;
  payload?: Record<string, unknown>;
  status: "pending" | "delivered";
  createdAt: string;
  deliveredAt: string | null;
}
