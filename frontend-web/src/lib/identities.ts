export const identities = ["Azure", "Burgundy", "Emerald", "Graphite", "Gray", "Indigo", "Navy", "Olive", "Plum", "Teal"] as const;

export type Identity = (typeof identities)[number];
