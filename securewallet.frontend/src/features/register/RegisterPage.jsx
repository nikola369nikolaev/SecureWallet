import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { registerUser } from '../../api/authApi';
import { ApiError } from '../../api/httpClient';

const BG_PHONE_PREFIX = '+359';
const BG_PHONE_DIGITS_LENGTH = 9;

export function RegisterPage() {
  const navigate = useNavigate();
  const [formState, setFormState] = useState({
    username: '',
    email: '',
    password: '',
    confirmPassword: '',
    phoneNumber: '+359',
    firstName: '',
    lastName: '',
  });
  const [errorMessage, setErrorMessage] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isPasswordInfoOpen, setIsPasswordInfoOpen] = useState(false);
  const [isPasswordVisible, setIsPasswordVisible] = useState(false);
  const [isConfirmPasswordVisible, setIsConfirmPasswordVisible] = useState(false);

  function updateField(field, value) {
    setFormState((current) => ({
      ...current,
      [field]: value,
    }));
  }

  function updatePhoneNumber(value) {
    let nextValue = value;

    if (nextValue === '' || nextValue === '+') {
      updateField('phoneNumber', BG_PHONE_PREFIX);
      return;
    }

    if (nextValue.startsWith('+') && nextValue.length <= 4) {
      updateField('phoneNumber', BG_PHONE_PREFIX);
      return;
    }

    if (nextValue.startsWith(BG_PHONE_PREFIX)) {
      const subscriberNumber = nextValue
        .slice(BG_PHONE_PREFIX.length)
        .replace(/\D/g, '')
        .replace(/^0+/, '')
        .slice(0, BG_PHONE_DIGITS_LENGTH);

      updateField('phoneNumber', `${BG_PHONE_PREFIX}${subscriberNumber}`);
      return;
    }

    let digitsOnly = nextValue.replace(/\D/g, '').slice(0, BG_PHONE_DIGITS_LENGTH + 3);

    if (digitsOnly.startsWith('359')) {
      digitsOnly = digitsOnly.slice(3);
    }

    digitsOnly = digitsOnly.replace(/^0+/, '').slice(0, BG_PHONE_DIGITS_LENGTH);

    updateField('phoneNumber', `${BG_PHONE_PREFIX}${digitsOnly}`);
  }

  function validatePhoneNumber() {
    const subscriberNumber = formState.phoneNumber.startsWith(BG_PHONE_PREFIX)
      ? formState.phoneNumber.slice(BG_PHONE_PREFIX.length)
      : '';

    return subscriberNumber.length === BG_PHONE_DIGITS_LENGTH && subscriberNumber[0] !== '0';
  }

  function hasMissingRequiredFields() {
    return (
      formState.username.trim() === '' ||
      formState.email.trim() === '' ||
      formState.password.trim() === '' ||
      formState.confirmPassword.trim() === '' ||
      formState.firstName.trim() === '' ||
      formState.lastName.trim() === ''
    );
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setErrorMessage('');

    if (hasMissingRequiredFields()) {
      setErrorMessage('Липсват данни.');
      return;
    }

    if (!validatePhoneNumber()) {
      setErrorMessage('Телефонният номер трябва да съдържа точно 9 цифри след +359 и първата цифра след +359 не може да бъде 0.');
      return;
    }

    if (formState.password !== formState.confirmPassword) {
      setErrorMessage('Паролите не съвпадат.');
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await registerUser(formState);
      navigate('/register/verify-email', {
        replace: true,
        state: {
          email: result.email ?? formState.email,
          message: 'Регистрацията е успешна. Въведи кода от имейла, за да продължиш.',
        },
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
          След регистрация ще получиш код за потвърждение на имейла, а след това ще преминеш
          към задължителната настройка на двуфакторната защита.
        </p>
        <div className="hero-note-grid">
          <div className="hero-note">
            <strong>Имейл потвърждение</strong>
            <span>Изпращаме код до въведения имейл, независимо дали е abv.bg, gmail.com или друг домейн.</span>
          </div>
          <div className="hero-note">
            <strong>Сигурна парола</strong>
            <span>Изискваме поне 8 символа, поне една главна буква и поне една цифра.</span>
          </div>
        </div>
      </section>

      <section className="form-panel">
        <div className="panel-header">
          <p className="eyebrow">Регистрация</p>
          <h2>Създай акаунт</h2>
          <p>
            Попълни данните внимателно. След това ще потвърдим имейла и ще настроиш
            {' '}
            <span className="inline-label-with-info">
              <span>временен код</span>
              <span className="info-tooltip-badge" tabIndex={0}>
                i
                <span className="info-tooltip-content">
                  Това е временен 6-цифрен код от Google Authenticator, Microsoft Authenticator или друго подобно приложение.
                </span>
              </span>
            </span>
            .
          </p>
        </div>

        <form className="auth-form" onSubmit={handleSubmit}>
          <div className="field-grid">
            <label className="field-group">
              <span>Потребителско име</span>
              <input
                value={formState.username}
                onChange={(event) => updateField('username', event.target.value)}
              />
            </label>
            <label className="field-group">
              <span>Имейл</span>
              <input
                type="email"
                value={formState.email}
                onChange={(event) => updateField('email', event.target.value)}
                placeholder="nikola@example.com"
              />
            </label>
          </div>

          <div className="field-grid">
            <label className="field-group">
              <span className="field-label-row">
                <span>Парола</span>
                <span className={`info-tooltip ${isPasswordInfoOpen ? 'info-tooltip--open' : ''}`}>
                  <button
                    className="info-badge"
                    type="button"
                    aria-label="Правила за парола"
                    aria-expanded={isPasswordInfoOpen}
                    onClick={() => setIsPasswordInfoOpen((current) => !current)}
                  >
                    i
                  </button>
                  <span className="info-tooltip-panel">
                    Паролата трябва да е поне 8 символа, да има поне една главна буква и поне една цифра.
                  </span>
                </span>
              </span>
              <div className="password-input-wrapper">
                <input
                  type={isPasswordVisible ? 'text' : 'password'}
                  value={formState.password}
                  onChange={(event) => updateField('password', event.target.value)}
                />
                <button
                  className="password-toggle-button"
                  type="button"
                  onClick={() => setIsPasswordVisible((current) => !current)}
                  aria-label={isPasswordVisible ? 'Скрий паролата' : 'Покажи паролата'}
                  title={isPasswordVisible ? 'Скрий паролата' : 'Покажи паролата'}
                >
                  👁
                </button>
              </div>
            </label>
            <label className="field-group">
              <span>Потвърди парола</span>
              <div className="password-input-wrapper">
                <input
                  type={isConfirmPasswordVisible ? 'text' : 'password'}
                  value={formState.confirmPassword}
                  onChange={(event) => updateField('confirmPassword', event.target.value)}
                />
                <button
                  className="password-toggle-button"
                  type="button"
                  onClick={() => setIsConfirmPasswordVisible((current) => !current)}
                  aria-label={isConfirmPasswordVisible ? 'Скрий потвърдената парола' : 'Покажи потвърдената парола'}
                  title={isConfirmPasswordVisible ? 'Скрий потвърдената парола' : 'Покажи потвърдената парола'}
                >
                  👁
                </button>
              </div>
            </label>
            <label className="field-group">
              <span>Телефон</span>
              <input
                value={formState.phoneNumber}
                onChange={(event) => updatePhoneNumber(event.target.value)}
                inputMode="numeric"
                maxLength={BG_PHONE_PREFIX.length + BG_PHONE_DIGITS_LENGTH}
                placeholder="+359888123456"
              />
            </label>
          </div>

          <div className="field-grid">
            <label className="field-group">
              <span>Собствено име</span>
              <input
                value={formState.firstName}
                onChange={(event) => updateField('firstName', event.target.value)}
                placeholder="Никола"
              />
            </label>
            <label className="field-group">
              <span>Фамилия</span>
              <input
                value={formState.lastName}
                onChange={(event) => updateField('lastName', event.target.value)}
                placeholder="Николаев"
              />
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
