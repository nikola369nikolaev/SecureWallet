import { useEffect, useMemo, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { createSupportAccount, getAdminUsers } from '../../api/adminApi';
import { ApiError } from '../../api/httpClient';
import { useAuth } from '../../auth/AuthContext';

function formatDate(value) {
  return new Intl.DateTimeFormat('bg-BG', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

function formatBooleanStatus(value, positiveText = 'Да', negativeText = 'Не') {
  return value ? positiveText : negativeText;
}

const initialSupportForm = {
  username: '',
  email: '',
  password: '',
  firstName: '',
  lastName: '',
  phoneNumber: '',
};

function getNextSupportNumber(users) {
  const supportNumbers = users
    .filter((user) => user.role === 'Support')
    .map((user) => {
      const match = user.username.match(/^support(\d+)$/i);
      return match ? Number(match[1]) : 0;
    });

  const maxNumber = supportNumbers.length ? Math.max(...supportNumbers) : 0;
  return maxNumber + 1;
}

function buildSuggestedSupportValues(users) {
  const nextNumber = getNextSupportNumber(users);

  return {
    username: `support${nextNumber}`,
    email: `secure.wallet-support${nextNumber}@abv.bg`,
  };
}

export function AdminUsersPage() {
  const { session, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [users, setUsers] = useState([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [form, setForm] = useState(initialSupportForm);
  const [isLoading, setIsLoading] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const isAdmin = session?.role === 'Admin';
  const isCreateSupportMode = isAdmin && location.hash === '#create-support';
  const suggestedSupportValues = useMemo(() => buildSuggestedSupportValues(users), [users]);
  const currentUserId = session?.userId;

  useEffect(() => {
    let isActive = true;

    async function loadUsers() {
      if (!session?.accessToken) {
        if (isActive) {
          setIsLoading(false);
        }
        return;
      }

      setIsLoading(true);
      setErrorMessage('');

      try {
        const result = await getAdminUsers(session.accessToken);

        if (!isActive) {
          return;
        }

        setUsers(result);
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

        setErrorMessage(error instanceof ApiError ? error.message : 'Възникна грешка при зареждане на потребителите.');
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    }

    loadUsers();

    return () => {
      isActive = false;
    };
  }, [logout, navigate, session?.accessToken]);

  const filteredUsers = useMemo(() => {
    const trimmedSearch = searchTerm.trim().toLowerCase();

    if (!trimmedSearch) {
      return users;
    }

    return users.filter((user) =>
      user.username.toLowerCase().includes(trimmedSearch) ||
      user.email.toLowerCase().includes(trimmedSearch) ||
      user.role.toLowerCase().includes(trimmedSearch),
    );
  }, [searchTerm, users]);

  function handleFormChange(fieldName, value) {
    setForm((current) => ({
      ...current,
      [fieldName]: value,
    }));
  }

  async function handleCreateSupportAccount(event) {
    event.preventDefault();
    setErrorMessage('');
    setSuccessMessage('');
    setIsCreating(true);

    try {
      const result = await createSupportAccount(
        {
          username: form.username,
          email: form.email,
          password: form.password,
          firstName: form.firstName,
          lastName: form.lastName,
          phoneNumber: form.phoneNumber.trim() ? form.phoneNumber : null,
        },
        session.accessToken,
      );

      setSuccessMessage(result.message);

      const refreshedUsers = await getAdminUsers(session.accessToken);
      setUsers(refreshedUsers);
      setForm(initialSupportForm);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'Възникна грешка при създаване на support акаунта.');
    } finally {
      setIsCreating(false);
    }
  }

  return (
    <main className="dashboard-page">
      <section className="dashboard-shell">
        <div className="dashboard-header">
          <div>
            <p className="eyebrow">{isAdmin ? 'Администрация' : 'Support преглед'}</p>
            <h1>{isCreateSupportMode ? 'Създай Support акаунт' : 'Регистрирани потребители'}</h1>
            <p className="dashboard-copy">
              {isCreateSupportMode
                ? 'Тук admin ролята създава support акаунти. Валидацията за дублиран имейл и потребителско име се прави в backend-а.'
                : `Тук ${isAdmin ? 'admin' : 'support'} ролята вижда позволените акаунти и може да отвори детайлите и историята на конкретен потребител.`}
            </p>
          </div>
          <Link className="secondary-link-button" to="/dashboard">
            Назад към началото
          </Link>
        </div>

        {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}
        {successMessage && <div className="message-box message-box--success">{successMessage}</div>}

        {isCreateSupportMode ? (
          <article className="dashboard-card admin-form-card">
            <form className="auth-form admin-inline-form" onSubmit={handleCreateSupportAccount}>
              <label className="field-group">
                <span>Потребителско име</span>
                <input
                  value={form.username}
                  onChange={(event) => handleFormChange('username', event.target.value)}
                  placeholder={suggestedSupportValues.username}
                  type="text"
                />
              </label>

              <label className="field-group">
                <span>Имейл</span>
                <input
                  value={form.email}
                  onChange={(event) => handleFormChange('email', event.target.value)}
                  placeholder={suggestedSupportValues.email}
                  type="email"
                />
              </label>

              <label className="field-group">
                <span>Парола</span>
                <input value={form.password} onChange={(event) => handleFormChange('password', event.target.value)} type="password" />
              </label>

              <label className="field-group">
                <span>Собствено име</span>
                <input value={form.firstName} onChange={(event) => handleFormChange('firstName', event.target.value)} type="text" />
              </label>

              <label className="field-group">
                <span>Фамилия</span>
                <input value={form.lastName} onChange={(event) => handleFormChange('lastName', event.target.value)} type="text" />
              </label>

              <label className="field-group">
                <span>Телефон по желание</span>
                <input value={form.phoneNumber} onChange={(event) => handleFormChange('phoneNumber', event.target.value)} type="text" />
              </label>

              <button className="primary-button" disabled={isCreating} type="submit">
                {isCreating ? 'Създаване...' : 'Създай support акаунт'}
              </button>
            </form>
          </article>
        ) : (
          <article className="dashboard-card">
            <div className="admin-table-header">
              <h2>Списък с акаунти</h2>
              <label className="field-group admin-search-field">
                <span>Търсене по потребителско име, имейл или роля</span>
                <input
                  type="text"
                  value={searchTerm}
                  onChange={(event) => setSearchTerm(event.target.value)}
                />
              </label>
            </div>

            {isLoading ? (
              <p className="field-hint">Зареждане...</p>
            ) : (
              <div className="table-wrapper">
                <table className="admin-table">
                  <thead>
                    <tr>
                      <th>Потребителско име</th>
                      <th>Имейл</th>
                      <th>Роля</th>
                      <th>Имейл статус</th>
                      <th>2FA</th>
                      <th>Активен</th>
                      <th>Създаден на</th>
                      <th>Детайли</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredUsers.map((user) => (
                      <tr key={user.userId}>
                        <td>{user.username}{user.userId === currentUserId ? ' (аз)' : ''}</td>
                        <td>{user.email}</td>
                        <td>{user.role}</td>
                        <td>{formatBooleanStatus(user.isEmailVerified, 'Потвърден', 'Непотвърден')}</td>
                        <td>{formatBooleanStatus(user.twoFactorEnabled, 'Включена', 'Изключена')}</td>
                        <td>{formatBooleanStatus(user.isActive, 'Да', 'Не')}</td>
                        <td>{formatDate(user.createdAtUtc)}</td>
                        <td>
                          <Link className="secondary-link-button secondary-link-button--compact" to={`/admin/users/${user.userId}`}>
                            Детайли
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>

                {!filteredUsers.length && <p className="field-hint">Няма резултати за това търсене.</p>}
              </div>
            )}
          </article>
        )}
      </section>
    </main>
  );
}
