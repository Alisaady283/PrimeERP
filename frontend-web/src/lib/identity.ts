import { create } from "zustand";
import { persist } from "zustand/middleware";
import { lasting } from "@/lib/platform";
import { identities, type Identity } from "@/lib/identities";

type IdentityState = { identity: Identity; setIdentity: (identity: string) => void };

export const useIdentity = create<IdentityState>()(
  persist(
    (set) => ({
      identity: "Navy",
      setIdentity: (identity) => set({ identity: (identities as readonly string[]).includes(identity) ? (identity as Identity) : "Navy" }),
    }),
    { name: "primeerp.identity", storage: lasting },
  ),
);
