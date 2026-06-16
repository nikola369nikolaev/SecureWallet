export function AppBrand({ subtitle = 'Защитен дигитален портфейл' }) {
  return (
    <div className="brand-lockup" aria-label="SecureWallet бранд">
      <img
        className="brand-lockup__logo"
        src="/securewallet-logo.png"
        alt="Лого на SecureWallet"
      />
      <div className="brand-lockup__text">
        <span className="brand-lockup__name">SecureWallet</span>
        <span className="brand-lockup__subtitle">{subtitle}</span>
      </div>
    </div>
  );
}
