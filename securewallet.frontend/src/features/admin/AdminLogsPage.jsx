import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getAdminLogs } from '../../api/adminApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';

function isHttpLogLine(line) {
  return line.includes('HTTP ') && line.includes('->');
}

function isInvalidCredentialWarningLine(line) {
  const lowerLine = line.toLowerCase();

  return lowerLine.includes('вход: защитна проверка за имейл') &&
    lowerLine.includes('грешен имейл или парола');
}

function isLockoutSecurityLine(line) {
  const lowerLine = line.toLowerCase();

  return lowerLine.includes('stage=lockoutactive') ||
    lowerLine.includes('lockoutseconds=') ||
    lowerLine.includes('твърде много неуспешни опити за вход') ||
    lowerLine.includes('опитай отново след');
}

function getSecurityAttemptCount(line) {
  const attemptsMatch = line.match(/Attempts=(\d+)/i);

  if (attemptsMatch) {
    return Number(attemptsMatch[1]);
  }

  const failedAttemptMatch = line.match(/опит номер:\s*(\d+)/i);

  if (failedAttemptMatch) {
    return Number(failedAttemptMatch[1]);
  }

  return null;
}

function isCriticalSecurityLogLine(line) {
  if (isLockoutSecurityLine(line)) {
    return true;
  }

  const attemptCount = getSecurityAttemptCount(line);

  if (attemptCount !== null && attemptCount >= 5) {
    return true;
  }

  if (isInvalidCredentialWarningLine(line)) {
    return false;
  }

  const lowerLine = line.toLowerCase();

  if (line.includes('[FTL]') || line.includes('[ERR]')) {
    return true;
  }

  if (!line.includes('[WRN]')) {
    return false;
  }

  return [
    'грешна парола',
    'невалидна парола',
    'невалиден код',
    'captcha',
    'капча',
    'lockout',
    'блок',
    'неуспеш',
    'reset',
    'ресет',
    'парола',
    'ddos',
    'подозр',
  ].some((keyword) => lowerLine.includes(keyword));
}

function isActionLogLine(line) {
  if (isHttpLogLine(line) || isCriticalSecurityLogLine(line) || isInvalidCredentialWarningLine(line)) {
    return false;
  }

  const lowerLine = line.toLowerCase();

  return [
    'транзакции:',
    'администрация:',
    'вход:',
    'регистрация:',
    'totp',
    'email verification',
    'имейл',
    'refresh',
    'парола:',
  ].some((keyword) => lowerLine.includes(keyword));
}

function getAuditLinePresentation(line) {
  if (isInvalidCredentialWarningLine(line)) {
    return {
      rowClassName: 'audit-entry audit-entry--warning',
      badgeClassName: 'audit-badge audit-badge--warning',
      badgeText: 'Опит',
    };
  }

  if (isCriticalSecurityLogLine(line)) {
    return {
      rowClassName: 'audit-entry audit-entry--critical',
      badgeClassName: 'audit-badge audit-badge--critical',
      badgeText: 'Критично',
    };
  }

  if (isActionLogLine(line)) {
    return {
      rowClassName: 'audit-entry audit-entry--action',
      badgeClassName: 'audit-badge audit-badge--action',
      badgeText: 'Действие',
    };
  }

  if (line.includes('[WRN]') || line.toLowerCase().includes('причина=')) {
    return {
      rowClassName: 'audit-entry audit-entry--warning',
      badgeClassName: 'audit-badge audit-badge--warning',
      badgeText: 'Предупр.',
    };
  }

  if (isHttpLogLine(line)) {
    return {
      rowClassName: 'audit-entry audit-entry--http',
      badgeClassName: 'audit-badge audit-badge--http',
      badgeText: 'HTTP',
    };
  }

  return {
    rowClassName: 'audit-entry audit-entry--neutral',
    badgeClassName: 'audit-badge audit-badge--neutral',
    badgeText: 'Лог',
  };
}

export function AdminLogsPage() {
  const { session, logout } = useAuth();
  const navigate = useNavigate();
  const [logResult, setLogResult] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');

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
        const result = await getAdminLogs(session.accessToken, 1000);

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
      const result = await getAdminLogs(session.accessToken, 1000);
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
                {logResult.lines.map((line, index) => {
                  const presentation = getAuditLinePresentation(line);

                  return (
                    <div className={presentation.rowClassName} key={`${index}-${line}`}>
                      <span className={presentation.badgeClassName}>{presentation.badgeText}</span>
                      <span className="audit-entry__text">{line}</span>
                    </div>
                  );
                })}
              </div>
            </div>
          )}
        </article>
      </section>
    </main>
  );
}

