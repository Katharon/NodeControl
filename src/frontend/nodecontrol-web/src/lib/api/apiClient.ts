export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message);
  }
}

async function apiRequest<TResponse>(
  path: string,
  init: RequestInit,
): Promise<TResponse> {
  const response = await fetch(path, {
    credentials: "include",
    headers: {
      Accept: "application/json",
      ...(init.body ? { "Content-Type": "application/json" } : {}),
      ...init.headers,
    },
    ...init,
  });

  if (!response.ok) {
    throw new ApiError(`Request failed with status ${response.status}`, response.status);
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  return (await response.json()) as TResponse;
}

export function apiGet<TResponse>(path: string): Promise<TResponse> {
  return apiRequest<TResponse>(path, { method: "GET" });
}

export function apiPost<TRequest, TResponse>(
  path: string,
  body: TRequest,
): Promise<TResponse> {
  return apiRequest<TResponse>(path, {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function apiPut<TRequest, TResponse>(
  path: string,
  body: TRequest,
): Promise<TResponse> {
  return apiRequest<TResponse>(path, {
    method: "PUT",
    body: JSON.stringify(body),
  });
}

export function apiDelete<TResponse>(path: string): Promise<TResponse> {
  return apiRequest<TResponse>(path, { method: "DELETE" });
}
