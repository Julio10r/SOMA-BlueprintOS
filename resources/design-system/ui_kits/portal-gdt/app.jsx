// AZZAS 2154 · Portal GDT — root router
// Uses globals: AuthScreen, AppShell, WelcomeScreen, TabPlaceholder, Toast

function App() {
  const data = window.GDT_DATA;
  const [session, setSession] = React.useState(null);   // null = not authed
  const [activeModule, setActiveModule] = React.useState(null);
  const [toast, setToast] = React.useState(null);

  // Auth flow
  if (!session) {
    return <AuthScreen data={data} onAuthed={(user) => setSession(user)} />;
  }

  const handleLogout = () => {
    setSession(null);
    setActiveModule(null);
  };

  return (
    <>
      <AppShell
        user={session}
        modules={data.modules}
        perfilMeta={data.perfilMeta}
        activeModule={activeModule}
        onNavigate={(key) => setActiveModule(key)}
        onLogout={handleLogout}
      >
        {!activeModule && (
          <WelcomeScreen user={session} modules={data.modules} onNavigate={setActiveModule} />
        )}
        {activeModule === "curadoria" && (
          <CuradoriaScreen data={data} onToast={setToast} />
        )}
        {activeModule === "avaliacao" && (
          <AvaliacaoScreen data={data} onToast={setToast} />
        )}
        {activeModule === "acompanhamento" && (
          <AcompanhamentoScreen data={data} />
        )}
        {activeModule && activeModule !== "curadoria" && activeModule !== "avaliacao" && activeModule !== "acompanhamento" && (
          <TabPlaceholder
            moduleKey={activeModule}
            moduleLabel={data.modules.find(m => m.key === activeModule)?.label}
          />
        )}
      </AppShell>
      {toast && <Toast kind={toast.kind} onDone={() => setToast(null)}>{toast.msg}</Toast>}
    </>
  );
}

ReactDOM.createRoot(document.getElementById("root")).render(<App />);
