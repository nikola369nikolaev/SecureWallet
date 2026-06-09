import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getAdminLogs } from '../../api/adminApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';

export function AdminLogsPage() {
  const { session, logout } = useAuth();
  const navigate = useNavigate();
  const [logResult, setLogResult] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');

  function getLogLineClassName(line) {
    if (line.includes('[FTL]')) {
      return 'log-line log-line--fatal';
    }

    if (line.includes('[ERR]')) {
      return 'log-line log-line--error';
    }

    if (line.includes('[WRN]')) {
      return 'log-line log-line--warning';
    }

    if (line.includes('[INF]')) {
      return 'log-line log-line--info';
    }

    return 'log-line';
  }

  function handleDownloadLogs() {
    if (!logResult?.lines?.length) {
      return;
    }

    const fileName = logResult.fileName?.endsWith('.log')
      ? logResult.fileName.replace(/\.log$/i, '.txt')
      : 'securewallet-logs.txt';

    const blob = new Blob([logResult.lines.join('\n')], { type: 'text/plain;charset=utf-8' });
    const downloadUrl = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(downloadUrl);
  }

  useEffect(() => {
    let isActive = true;

    async function loadLogs() {
      if (!session?.accessToken) {
        if (isActive) {
          setIsLoading(false);
        }
        return;
      }

      setIsLoading(true);
      setErrorMessage('');

      try {
        const result = await getAdminLogs(session.accessToken, 200);

        if (!isActive) {
          return;
        }

        setLogResult(result);
      } catch (error) {
        if (!isActive) {
          return;
        }

        if (error instanceof ApiError && error.status === 401) {
          logout();
          navigate('/login', {
            replace: true,
            state: { sessionExpired: true },
          });
          return;
        }

        setErrorMessage(error instanceof ApiError ? error.message : 'Възникна грешка при зареждане на логовете.');
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    }

    loadLogs();

    return () => {
      isActive = false;
    };
  }, [logout, navigate, session?.accessToken]);

  async function handleRefresh() {
    if (!session?.accessToken) {
      return;
    }

    setIsLoading(true);
    setErrorMessage('');

    try {
      const result = await getAdminLogs(session.accessToken, 200);
      setLogResult(result);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
        navigate('/login', {
          replace: true,
          state: { sessionExpired: true },
        });
        return;
      }

      setErrorMessage(error instanceof ApiError ? error.message : 'Възникна грешка при обновяване на логовете.');
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <main className="dashboard-page">
      <section className="dashboard-shell">
        <div className="dashboard-header">
          <div>
            <p className="eyebrow">Одит и мониторинг</p>
            <h1>Системни логове</h1>
            <p className="dashboard-copy">
              Тук Админ акаунтът вижда последните редове от одит логовете на приложението.
            </p>
          </div>
          <div className="inline-action-row">
            <button
              className="secondary-button"
              onClick={handleDownloadLogs}
              type="button"
              disabled={!logResult?.lines?.length}
            >
              Изтегли логовете
            </button>
            <button className="secondary-button" onClick={handleRefresh} type="button" disabled={isLoading}>
              {isLoading ? 'Обновяване...' : 'Обнови'}
            </button>
            <Link className="secondary-link-button" to="/dashboard">
              Назад към началото
            </Link>
          </div>
        </div>

        {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}

        <article className="dashboard-card">
          <div className="admin-table-header">
            <div>
              <h2>Последни лог записи</h2>
              <p className="field-hint">
                Файл: {logResult?.fileName ?? '-'}
              </p>
              <p className="field-hint">
                Папка: {logResult?.logDirectory ?? '-'}
              </p>
              <p className="field-hint">
                Показани редове: {logResult?.returnedLineCount ?? 0}
              </p>
            </div>
          </div>

          {isLoading ? (
            <p className="field-hint">Зареждане...</p>
          ) : !logResult?.lines?.length ? (
            <p className="field-hint">Все още няма налични лог записи.</p>
          ) : (
            <div className="log-viewer-shell">
              <div className="log-viewer-output">
                {logResult.lines.map((line, index) => (
                  <div className={getLogLineClassName(line)} key={`${index}-${line}`}>
                    {line}
                  </div>
                ))}
              </div>
            </div>
          )}
        </article>
      </section>
    </main>
  );
}
