export async function requestOptionalJson<T>(
  input: RequestInfo | URL,
  init?: RequestInit,
): Promise<{ ok: boolean; status: number; data?: T; text: string }> {
  try {
    const response = await fetch(input, init);
    const text = await response.text();
    let data: T | undefined;
    if (text) {
      try {
        data = JSON.parse(text) as T;
      } catch {
        data = undefined;
      }
    }
    return { ok: response.ok, status: response.status, data, text };
  } catch (err) {
    return {
      ok: false,
      status: 0,
      text: err instanceof Error ? err.message : "Request failed.",
    };
  }
}

export async function requestFirstAvailableJson<T>(
  inputs: string[],
): Promise<{ ok: boolean; status: number; data?: T; text: string; source?: string }> {
  let last: { ok: boolean; status: number; data?: T; text: string; source?: string } = {
    ok: false,
    status: 0,
    text: "No endpoint was checked.",
  };

  for (const input of inputs) {
    const result = await requestOptionalJson<T>(input, { cache: "no-store" });
    last = { ...result, source: input };
    if (result.ok || !isPendingEndpoint(result.status)) {
      return last;
    }
  }

  return last;
}

export async function requestOptionalCollection<T>(
  input: string,
  keys: string[],
): Promise<{ ok: boolean; status: number; data: T[]; text: string; source?: string }> {
  const result = await requestOptionalJson<unknown>(input, { cache: "no-store" });
  return {
    ok: result.ok,
    status: result.status,
    data: result.ok ? extractCollection<T>(result.data, keys) : [],
    text: result.text,
    source: input,
  };
}

export async function requestFirstAvailableCollection<T>(
  inputs: string[],
  keys: string[],
): Promise<{ ok: boolean; status: number; data: T[]; text: string; source?: string }> {
  let last: { ok: boolean; status: number; data: T[]; text: string; source?: string } = {
    ok: false,
    status: 0,
    data: [],
    text: "No endpoint was checked.",
  };

  for (const input of inputs) {
    const result = await requestOptionalCollection<T>(input, keys);
    last = result;
    if (result.ok || !isPendingEndpoint(result.status)) {
      return result;
    }
  }

  return last;
}

export function extractCollection<T>(data: unknown, keys: string[]): T[] {
  if (Array.isArray(data)) {
    return data as T[];
  }

  if (!data || typeof data !== "object") {
    return [];
  }

  const record = data as Record<string, unknown>;
  for (const key of keys) {
    const value = record[key];
    if (Array.isArray(value)) {
      return value as T[];
    }
  }

  return [];
}

export function extractNestedObject<T>(data: unknown, keys: string[]): T | null {
  if (!data || typeof data !== "object") {
    return null;
  }

  const record = data as Record<string, unknown>;
  for (const key of keys) {
    const value = record[key];
    if (value && typeof value === "object" && !Array.isArray(value)) {
      return value as T;
    }
  }

  return null;
}

export function isPendingEndpoint(status: number) {
  return status === 0 || status === 404 || status === 405 || status === 501;
}
