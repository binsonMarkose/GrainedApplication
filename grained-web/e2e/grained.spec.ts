import { test, expect, type Page } from '@playwright/test'

const SUPER = { email: 'superadmin@grained.org', password: 'ChangeMe123!' }
const ADMIN = { email: 'admin@gracecommunity.org', password: 'ChangeMe123!' }

async function login(page: Page, email: string, password: string) {
  await page.goto('/login')
  await page.fill('input[type=email]', email)
  await page.fill('input[type=password]', password)
  await page.click('button[type=submit]')
  await page.waitForURL('/')
  await expect(page.getByText('Log out')).toBeVisible()
}

test.describe('Grained admin', () => {
  test('church admin logs in and sees the dashboard', async ({ page }) => {
    await login(page, ADMIN.email, ADMIN.password)
    await expect(page.getByRole('heading', { name: /Welcome back/ })).toBeVisible()
    await expect(page.getByText('Published lessons')).toBeVisible()
    // exact:true avoids also matching the sidebar "Class Groups" nav link
    await expect(page.getByText('Class groups', { exact: true })).toBeVisible()
  })

  test('list pages render for an admin', async ({ page }) => {
    await login(page, ADMIN.email, ADMIN.password)
    for (const [path, heading] of [
      ['/children', 'Children'],
      ['/lessons', 'Lessons'],
      ['/attendance', 'Attendance'],
      ['/reports', 'Reports'],
      ['/class-groups', 'Class Groups'],
    ] as const) {
      await page.goto(path)
      await expect(page.getByRole('heading', { name: heading })).toBeVisible()
    }
  })

  test('bad password is rejected', async ({ page }) => {
    await page.goto('/login')
    await page.fill('input[type=email]', ADMIN.email)
    await page.fill('input[type=password]', 'wrong-password')
    await page.click('button[type=submit]')
    await expect(page.getByText(/Invalid email or password/)).toBeVisible()
  })
})

test.describe('Mobile navigation', () => {
  test.use({ viewport: { width: 430, height: 932 }, isMobile: true, hasTouch: true })

  test('hamburger opens the drawer and navigates', async ({ page }) => {
    await login(page, ADMIN.email, ADMIN.password)
    await page.goto('/children')
    const burger = page.getByRole('button', { name: 'Open menu' })
    await expect(burger).toBeVisible()
    await burger.click()
    const lessons = page.getByRole('link', { name: /Lessons/ })
    await expect(lessons).toBeVisible()
    await lessons.click()
    await expect(page).toHaveURL('/lessons')
  })
})

test.describe('Password reset', () => {
  // Non-destructive: resets the admin back to the same seeded password, so other tests are unaffected.
  test('forgot password → reset link → set new password → sign in', async ({ page }) => {
    await page.goto('/login')
    await page.getByRole('link', { name: /Forgot your password/ }).click()
    await expect(page).toHaveURL('/forgot-password')

    await page.fill('input[type=email]', ADMIN.email)
    await page.getByRole('button', { name: /Send reset link/ }).click()
    await expect(page.getByText(/we've sent a link/)).toBeVisible()

    const resetUrl = await page.locator('input[readonly]').inputValue()
    expect(resetUrl).toContain('/reset-password?')

    await page.goto(resetUrl)
    await expect(page.getByRole('heading', { name: /Set a new password/ })).toBeVisible()
    await page.locator('label:has-text("New password") input').fill(ADMIN.password)
    await page.locator('label:has-text("Confirm password") input').fill(ADMIN.password)
    await page.getByRole('button', { name: /Update password/ }).click()

    await expect(page.getByText(/password has been updated/)).toBeVisible()
    await page.getByRole('link', { name: /Sign in/ }).click()
    await expect(page).toHaveURL('/login')

    await login(page, ADMIN.email, ADMIN.password)
    await expect(page.getByRole('heading', { name: /Welcome back/ })).toBeVisible()
  })
})

test.describe('Church onboarding', () => {
  test('super admin onboards a church; admin accepts and lands on the dashboard', async ({ page, browser }) => {
    const email = `e2e.pastor.${Date.now()}@example.com`

    // 1. SuperAdmin provisions a church with name + email
    await login(page, SUPER.email, SUPER.password)
    await page.goto('/churches')
    await page.getByRole('button', { name: /Onboard a church/ }).click()
    await page.locator('label:has-text("Church name") input').fill('E2E Test Church')
    await page.fill('input[placeholder="pastor@church.org"]', email)
    // submit via Enter (avoids modal-overlay click flakiness)
    await page.locator('input[placeholder="pastor@church.org"]').press('Enter')

    await expect(page.getByText('Invite sent')).toBeVisible()
    const inviteLink = await page.locator('input[readonly]').inputValue()
    expect(inviteLink).toContain('/accept-invite?token=')

    // 2. The invited admin opens the link in a clean context and completes onboarding
    const ctx = await browser.newContext()
    const p2 = await ctx.newPage()
    await p2.goto(inviteLink)
    await expect(p2.getByText("You're setting up")).toBeVisible()
    await expect(p2.getByText('E2E Test Church')).toBeVisible()

    await p2.locator('label:has-text("First name") input').fill('Testy')
    await p2.locator('label:has-text("Last name") input').fill('McTest')
    await p2.locator('label:has-text("Create a password") input').fill('Onboard123!')
    await p2.locator('label:has-text("Confirm password") input').fill('Onboard123!')
    await p2.locator('label:has-text("Confirm password") input').press('Enter')

    await p2.waitForURL('/')
    await expect(p2.getByRole('heading', { name: /Welcome back, Testy/ })).toBeVisible()
    await ctx.close()
  })
})
