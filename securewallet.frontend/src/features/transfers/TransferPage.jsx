import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getCurrentWallet } from '../../api/walletApi';
import { createTransfer } from '../../api/transactionApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';

const recipientTypeOptions = [
  {
    value: 'Username',
    label: 'Потребителско име',
    placeholder: 'nikola.demo',
    hint: 'Използвай уникалното потребителско име на получателя.',
  },
  {
    value: 'PhoneNumber',
    label: 'Телефон',
    placeholder: '+359888123456',
    hint: 'Ако има повече от един акаунт с този номер, backend-ът ще откаже превода.',
  },
  {
    value: 'Iban',
    label: 'IBAN',
    placeholder: 'BG80BNBG96611020345678',
    hint: 'Можеш да превеждаш и директно по IBAN на портфейла.',
  },
];

function formatBalance(balance, currency) {
  return `${new Intl.NumberFormat('bg-BG', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(balance)} ${currency}`;
}

export function TransferPage() {
  const { session, logout } = useAuth();
  const navigate = useNavigate();
  const [wallet, setWallet] = useState(null);
  const [formState, setFormState] = useState({
    recipientType: 'Username',
    recipientValue: '',
    amount: '',
    description: '',
  });
  const [transferTotpCode, setTransferTotpCode] = useState('');
  const [pendingTransferPayload, setPendingTransferPayload] = useState(null);
  const [errorMessage, setErrorMessage] = useState('');
  const [confirmErrorMessage, setConfirmErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [transferReference, setTransferReference] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isConfirmModalOpen, setIsConfirmModalOpen] = useState(false);

  useEffect(() => {
    let isActive = true;

    async function loadWallet() {
      if (!session?.accessToken) {
        if (isActive) {
          setIsLoading(false);
        }
        return;
      }

      setIsLoading(true);
      setErrorMessage('');

      try {
        const result = await getCurrentWallet(session.accessToken);

        if (!isActive) {
          return;
        }

        setWallet(result);
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

        setErrorMessage(error instanceof ApiError ? error.message : 'Възникна проблем при зареждане на баланса. Опитай отново.');
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    }

    loadWallet();

    return () => {
      isActive = false;
    };
  }, [logout, navigate, session?.accessToken]);

  function updateField(field, value) {
    setFormState((current) => ({
      ...current,
      [field]: value,
    }));
  }

  function openTransferConfirmation(event) {
    event?.preventDefault();
    setErrorMessage('');
    setConfirmErrorMessage('');
    setSuccessMessage('');
    setTransferReference('');

    const amount = Number(formState.amount.replace(',', '.'));
    const recipientValue = formState.recipientValue.trim();
    const description = formState.description.trim();

    if (!recipientValue) {
      setErrorMessage('Полето за получател е задължително.');
      return;
    }

    if (!Number.isFinite(amount) || amount <= 0) {
      setErrorMessage('Сумата трябва да е по-голяма от 0.');
      return;
    }

    if (wallet && amount > wallet.balance) {
      setErrorMessage('Нямаш достатъчен баланс за този превод.');
      return;
    }

    setPendingTransferPayload({
      recipientType: formState.recipientType,
      recipientValue,
      amount,
      description,
    });
    setTransferTotpCode('');
    setIsConfirmModalOpen(true);
  }

  function closeTransferConfirmation() {
    if (isSubmitting) {
      return;
    }

    setIsConfirmModalOpen(false);
    setTransferTotpCode('');
    setConfirmErrorMessage('');
    setPendingTransferPayload(null);
  }

  async function handleConfirmTransfer() {
    if (!pendingTransferPayload) {
      return;
    }

    setErrorMessage('');
    setConfirmErrorMessage('');
    setSuccessMessage('');
    setTransferReference('');
    setIsSubmitting(true);

    try {
      const result = await createTransfer(
        {
          ...pendingTransferPayload,
          description: pendingTransferPayload.description || null,
          totpCode: transferTotpCode,
        },
        session.accessToken,
      );

      setSuccessMessage(result.message);
      setTransferReference(result.reference);
      setWallet((current) =>
        current
          ? {
              ...current,
              balance: result.updatedBalance,
            }
          : current,
      );
      setFormState((current) => ({
        recipientType: current.recipientType,
        recipientValue: '',
        amount: '',
        description: '',
      }));
      setTransferTotpCode('');
      setPendingTransferPayload(null);
      setIsConfirmModalOpen(false);
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.status === 401) {
          logout();
          navigate('/login', {
            replace: true,
            state: { sessionExpired: true },
          });
          return;
        }

        setConfirmErrorMessage(error.payload?.message ?? error.message);
      } else {
        setConfirmErrorMessage('Възникна проблем при изпращане на превода. Опитай отново.');
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  const selectedRecipientType = recipientTypeOptions.find((option) => option.value === formState.recipientType) ?? recipientTypeOptions[0];

  return (
    <main className="dashboard-page">
      <section className="dashboard-shell">
        <div className="dashboard-header">
          <div>
            <p className="eyebrow">Преводи</p>
            <h1>Нов превод към потребител, телефон или IBAN</h1>
          </div>
          <Link className="secondary-link-button" to="/dashboard">
            Назад към началото
          </Link>
        </div>

        <div className="inline-action-row">
          <Link className="secondary-link-button" to="/transactions">
            История
          </Link>
          <Link className="secondary-link-button" to="/settings">
            Още
          </Link>
        </div>

        <div className="dashboard-grid">
          <article className="dashboard-card transfer-balance-card">
            <p className="eyebrow">Портфейл</p>
            <h2>Наличност</h2>

            <div className="transfer-balance-hero">
              <span className="transfer-balance-hero__label">Текущ баланс</span>
              <strong className="transfer-balance-hero__amount">
                {isLoading || !wallet ? 'Зареждане...' : formatBalance(wallet.balance, wallet.currency)}
              </strong>
            </div>

            <div className="transfer-balance-grid">
              <div className="transfer-balance-tile">
                <span className="transfer-balance-tile__label">Валута</span>
                <strong>{isLoading || !wallet ? 'Зареждане...' : wallet.currency}</strong>
              </div>

              <div className="transfer-balance-tile">
                <span className="transfer-balance-tile__label">Статус</span>
                <strong>{isLoading ? 'Зареждане...' : 'Готов за превод'}</strong>
              </div>
            </div>

            <p className="field-hint">
              
            </p>
          </article>

          <article className="dashboard-card">
            <h2>Начин на превод</h2>

            <form className="auth-form" onSubmit={openTransferConfirmation}>
              <div className="recipient-type-grid">
                {recipientTypeOptions.map((option) => (
                  <button
                    key={option.value}
                    className={formState.recipientType === option.value ? 'recipient-type-button recipient-type-button--active' : 'recipient-type-button'}
                    type="button"
                    onClick={() => updateField('recipientType', option.value)}
                  >
                    {option.label}
                  </button>
                ))}
              </div>

              <label className="field-group">
                <span>{selectedRecipientType.label} на получателя</span>
                <input
                  value={formState.recipientValue}
                  onChange={(event) => updateField('recipientValue', event.target.value)}
                  placeholder={selectedRecipientType.placeholder}
                />
              </label>

              <p className="field-hint">{selectedRecipientType.hint}</p>

              <label className="field-group">
                <span>Сума</span>
                <input
                  value={formState.amount}
                  onChange={(event) => updateField('amount', event.target.value)}
                  placeholder="25.00"
                  inputMode="decimal"
                />
              </label>

              <label className="field-group">
                <span>Коментар</span>
                <input
                  value={formState.description}
                  onChange={(event) => updateField('description', event.target.value)}
                  placeholder="За вечеря"
                />
              </label>

              {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}
              {successMessage && (
                <div className="message-box message-box--success">
                  {successMessage}
                  {transferReference ? ` Референция: ${transferReference}` : ''}
                </div>
              )}

              <button className="primary-button" type="button" onClick={openTransferConfirmation} disabled={isSubmitting}>
                Продължи към потвърждение
              </button>
            </form>
          </article>
        </div>
      </section>

      {isConfirmModalOpen && pendingTransferPayload && (
        <div className="modal-backdrop" onClick={closeTransferConfirmation}>
          <article className="dashboard-card modal-card" onClick={(event) => event.stopPropagation()}>
            <div className="dashboard-header dashboard-header--compact">
              <div>
                <p className="eyebrow">Потвърждение</p>
                <h2>Потвърди превода с временен код</h2>
                <p className="dashboard-copy">
                  Всеки превод се завършва само след валиден временен код.
                </p>
              </div>
              <button className="secondary-button" type="button" onClick={closeTransferConfirmation} disabled={isSubmitting}>
                Затвори
              </button>
            </div>

            <div className="message-box message-box--info">
              Получател: <strong>{pendingTransferPayload.recipientValue}</strong><br />
              Сума: <strong>{formatBalance(pendingTransferPayload.amount, wallet?.currency ?? 'EUR')}</strong>
              {pendingTransferPayload.description && (
                <>
                  <br />
                  Коментар: <strong>{pendingTransferPayload.description}</strong>
                </>
              )}
            </div>

            <div className="auth-form">
              <label className="field-group">
                <span className="inline-label-with-info">
                  <span>Временен код</span>
                  <span className="info-tooltip-badge" tabIndex={0}>
                    i
                    <span className="info-tooltip-content">
                      Това е временен 6-цифрен код от Google Authenticator, Microsoft Authenticator или друго подобно приложение.
                    </span>
                  </span>
                </span>
                <input
                  type="text"
                  value={transferTotpCode}
                  onChange={(event) => setTransferTotpCode(event.target.value)}
                  placeholder="123456"
                  inputMode="numeric"
                />
              </label>

              {confirmErrorMessage && <div className="message-box message-box--error">{confirmErrorMessage}</div>}

              <div className="inline-action-row">
                <button className="primary-button" type="button" onClick={handleConfirmTransfer} disabled={isSubmitting}>
                  {isSubmitting ? 'Изпращане...' : 'Изпрати превода'}
                </button>
                <button className="secondary-button" type="button" onClick={closeTransferConfirmation} disabled={isSubmitting}>
                  Отказ
                </button>
              </div>
            </div>
          </article>
        </div>
      )}
    </main>
  );
}
