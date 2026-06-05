import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { registerUser } from '../../api/authApi';
import { ApiError } from '../../api/httpClient';

export function RegisterPage() {
  const navigate = useNavigate();
  const [formState, setFormState] = useState({
    username: '',
    email: '',
    password: '',
    phoneNumber: '',
    firstName: '',
    lastName: '',
  });
  const [errorMessage, setErrorMessage] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  function updateField(field, value) {
    setFormState((current) => ({
      ...current,
      [field]: value,
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setIsSubmitting(true);
    setErrorMessage('');

    try {
      await registerUser(formState);
      navigate('/login', {
        replace: true,
        state: { email: formState.email },
      });
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.payload?.message ?? error.message);
      } else {
        setErrorMessage('Възникна неочаквана грешка при регистрация.');
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="auth-page auth-page--register">
      <section className="hero-panel hero-panel--warm">
        <p className="eyebrow">SecureWallet</p>
        <h1>Създай профил и влез в своя дигитален портфейл.</h1>
        <p className="hero-copy">
          Формата за регистрация е свързана директно с backend validation слоя. След успешна
          регистрация системата създава потребител и начален портфейл за него.
        </p>
        <div className="hero-note-grid">
          <div className="hero-note">
            <strong>Сигурна парола</strong>
            <span>Изискваме 8 символа, поне една главна буква и поне една цифра.</span>
          </div>
          <div className="hero-note">
            <strong>Телефон</strong>
            <span>Телефонът започва с + и по-късно ще ни трябва за SMS защита.</span>
          </div>
        </div>
      </section>

      <section className="form-panel">
        <div className="panel-header">
          <p className="eyebrow">Регистрация</p>
          <h2>Създай акаунт</h2>
          <p>Попълни данните внимателно. Backend-ът ще създаде и началния wallet.</p>
        </div>

        <form className="auth-form" onSubmit={handleSubmit}>
          <div className="field-grid">
            <label className="field-group">
              <span>Потребителско име</span>
              <input value={formState.username} onChange={(event) => updateField('username', event.target.value)} placeholder="nikola.demo" />
            </label>
            <label className="field-group">
              <span>Email</span>
              <input type="email" value={formState.email} onChange={(event) => updateField('email', event.target.value)} placeholder="nikola@example.com" />
            </label>
          </div>

          <div className="field-grid">
            <label className="field-group">
              <span className="field-label-row">
                <span>Парола</span>
                <span
                  className="info-badge"
                  title="Паролата трябва да е поне 8 символа, да има поне една главна буква и поне една цифра."
                  aria-label="Правила за парола"
                >
                  i
                </span>
              </span>
              <input type="password" value={formState.password} onChange={(event) => updateField('password', event.target.value)} placeholder="Password1" />
            </label>
            <label className="field-group">
              <span>Телефон</span>
              <input value={formState.phoneNumber} onChange={(event) => updateField('phoneNumber', event.target.value)} placeholder="+359888123456" />
            </label>
          </div>

          <div className="field-grid">
            <label className="field-group">
              <span>Собствено име</span>
              <input value={formState.firstName} onChange={(event) => updateField('firstName', event.target.value)} placeholder="Никола" />
            </label>
            <label className="field-group">
              <span>Фамилия</span>
              <input value={formState.lastName} onChange={(event) => updateField('lastName', event.target.value)} placeholder="Николаев" />
            </label>
          </div>

          {errorMessage && <div className="message-box message-box--error">{errorMessage}</div>}

          <button className="primary-button" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Изпращане...' : 'Създай профил'}
          </button>
        </form>

        <p className="panel-footer">
          Вече имаш профил? <Link to="/login">Влез оттук</Link>
        </p>
      </section>
    </main>
  );
}