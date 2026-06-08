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
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [transferReference, setTransferReference] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

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

        setErrorMessage(error instanceof ApiError ? error.message : 'Възникна грешка при зареждане на баланса.');
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

  async function handleSubmit(event) {
    event.preventDefault();
    setErrorMessage('');
    setSuccessMessage('');
    setTransferReference('');
    setIsSubmitting(true);

    try {
      const amount = Number(formState.amount.replace(',', '.'));
      const result = await createTransfer(
        {
          recipientType: formState.recipientType,
          recipientValue: formState.recipientValue,
          amount,
          description: formState.description,
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
      setFormState({
        recipientType: formState.recipientType,
        recipientValue: '',
        amount: '',
        description: '',
      });
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

        setErrorMessage(error.payload?.message ?? error.message);
      } else {
        setErrorMessage('Възникна неочаквана грешка при изпращане на превода.');
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
            <p className="dashboard-copy">
              Backend-ът решава към кой акаунт отива преводът според избрания тип получател и не позволява неясни съвпадения.
            </p>
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
          <article className="dashboard-card">
            <h2>Наличност</h2>
            <dl>
              <div>
                <dt>Текущ баланс</dt>
                <dd>{isLoading || !wallet ? 'Зареждане...' : formatBalance(wallet.balance, wallet.currency)}</dd>
              </div>
              <div>
                <dt>Валута</dt>
                <dd>{isLoading || !wallet ? 'Зареждане...' : wallet.currency}</dd>
              </div>
            </dl>
          </article>

          <article className="dashboard-card">
            <h2>Данни за превода</h2>

            <form className="auth-form" onSubmit={handleSubmit}>
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

              <button className="primary-button" type="submit" disabled={isSubmitting}>
                {isSubmitting ? 'Изпращане...' : 'Изпрати превода'}
              </button>
            </form>
          </article>
        </div>
      </section>
    </main>
  );
}
