import { useState, useEffect } from 'react'
import './App.css'

const API_URL = 'http://localhost:5183'

function App() {
  const [data, setData] = useState(null)
  const [error, setError] = useState(null)

  useEffect(() => {
    fetch(`${API_URL}/weatherforecast`)
        .then((r) => {
          if (!r.ok) throw new Error(`HTTP ${r.status}`)
          return r.json()
        })
        .then(setData)
        .catch((e) => setError(e.message))
  }, [])

  return (
      <div style={{ padding: 24, fontFamily: 'sans-serif' }}>
        <h1>F1 Hub</h1>
        <h2>Перевірка зв'язку з API</h2>

        {!data && !error && <p>Завантаження...</p>}

        {error && (
            <p style={{ color: 'red' }}>
              Помилка: {error}. Перевір, що API запущений і налаштований CORS.
            </p>
        )}

        {data && (
            <>
              <p style={{ color: 'green' }}>✅ API відповідає</p>
              <ul>
                {data.map((d) => (
                    <li key={d.date}>
                      {d.date}: {d.temperatureC}°C, {d.summary}
                    </li>
                ))}
              </ul>
            </>
        )}
      </div>
  )
}

export default App