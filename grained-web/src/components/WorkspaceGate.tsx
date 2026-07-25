import { useAuth } from '../auth/AuthContext'
import { AppShell } from './AppShell'
import { ChooseWorkspace } from '../pages/ChooseWorkspace'
import { SetInitialPassword } from '../pages/SetInitialPassword'

// Gates before the app: (1) if the account still has an admin-issued temporary code, force the user
// to set their own password; (2) if they can be in more than one workspace and haven't picked one,
// show the chooser; otherwise render the normal app shell.
export function WorkspaceGate() {
  const { user, workspaceOptions, workspace } = useAuth()
  if (user?.mustChangePassword) return <SetInitialPassword />
  if (workspaceOptions.length > 1 && !workspace) return <ChooseWorkspace />
  return <AppShell />
}
