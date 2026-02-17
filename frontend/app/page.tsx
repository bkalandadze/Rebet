'use client';

import { useEffect, useState } from 'react';

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8080';

export default function Home() {
  const [health, setHealth] = useState<{ status: string; error?: string } | null>(null);

  useEffect(() => {
    fetch(`${apiUrl}/health`)
      .then(async (res) => {
        const text = await res.text();
        try {
          const data = JSON.parse(text);
          return (data.status ?? text) || 'unknown';
        } catch {
          return text || (res.ok ? 'Healthy' : 'Unhealthy');
        }
      })
      .then((status) => setHealth({ status }))
      .catch((err) => setHealth({ status: 'error', error: err.message }));
  }, []);

  return (
    <main>
      <h1>Betting Platform</h1>
      <p>Sample Next.js frontend — monorepo with backend API.</p>
      <section>
        <h2>API health</h2>
        <p>
          Endpoint: <code>{apiUrl}/health</code>
        </p>
        {health ? (
          <p className={health.status === 'Healthy' ? 'status-ok' : 'status-error'}>
            {health.status}
            {health.error && ` — ${health.error}`}
          </p>
        ) : (
          <p>Checking…</p>
        )}
      </section>
    </main>
  );
}
