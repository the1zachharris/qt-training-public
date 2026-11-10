import { expect, test } from '@playwright/test';
import { API_URL } from '../playwright.config';
import { uniqueName } from '../tests/helpers';

const ORDERS = `${API_URL}/api/v1/orders`;
const pizza = (over: Record<string, unknown> = {}) => ({ presetPizzaId: null, sizeId: 1, toppingIds: [], quantity: 1, ...over });

test.describe('API: menu and health', () => {
  test('menu matches requirements.md', async ({ request }) => {
    const menu = await (await request.get(`${API_URL}/api/v1/menu`)).json();
    expect(menu.sizes.map((s: any) => [s.name, s.inches, s.price])).toEqual([['Small', 10, 10], ['Large', 14, 14]]);
    expect(menu.toppings.map((t: any) => [t.name, t.price])).toEqual([
      ['Pepperoni', 2], ['Mushrooms', 2], ['Onions', 2], ['Green peppers', 2],
    ]);
  });

  test('health endpoint answers 200', async ({ request }) => {
    expect((await request.get(`${API_URL}/api/v1/health`)).status()).toBe(200);
  });

  test('observation: Swagger UI and OpenAPI document are reachable', async ({ request }) => {
    const swagger = await request.get(`${API_URL}/swagger/index.html`);
    const openapi = await request.get(`${API_URL}/openapi/v1.json`);
    test.info().annotations.push({ type: 'observation', description: `swagger ${swagger.status()}, openapi ${openapi.status()} (Development environment)` });
  });
});

test.describe('API: placing orders is validated', () => {
  const bad: [string, unknown][] = [
    ['empty name', { customerName: '', pizzas: [pizza()] }],
    ['whitespace name', { customerName: '   ', pizzas: [pizza()] }],
    ['null name', { customerName: null, pizzas: [pizza()] }],
    ['101-char name', { customerName: 'A'.repeat(101), pizzas: [pizza()] }],
    ['no pizzas', { customerName: 'Pat', pizzas: [] }],
    ['null pizzas', { customerName: 'Pat', pizzas: null }],
    ['null pizza entry', { customerName: 'Pat', pizzas: [null] }],
    ['21 lines', { customerName: 'Pat', pizzas: Array.from({ length: 21 }, () => pizza()) }],
    ['quantity 0', { customerName: 'Pat', pizzas: [pizza({ quantity: 0 })] }],
    ['quantity 21', { customerName: 'Pat', pizzas: [pizza({ quantity: 21 })] }],
    ['quantity -5', { customerName: 'Pat', pizzas: [pizza({ quantity: -5 })] }],
    ['unknown size', { customerName: 'Pat', pizzas: [pizza({ sizeId: 99 })] }],
    ['unknown topping', { customerName: 'Pat', pizzas: [pizza({ toppingIds: [99] })] }],
    ['duplicate topping', { customerName: 'Pat', pizzas: [pizza({ toppingIds: [1, 1] })] }],
    ['unknown preset', { customerName: 'Pat', pizzas: [pizza({ presetPizzaId: 99 })] }],
    ['quantity as string', { customerName: 'Pat', pizzas: [pizza({ quantity: 'two' })] }],
    ['empty object', {}],
  ];

  for (const [label, body] of bad) {
    test(`rejects ${label} with 400`, async ({ request }) => {
      const response = await request.post(ORDERS, { data: body });
      const text = await response.text();
      test.info().annotations.push({ type: 'response', description: `${response.status()} ${text.slice(0, 300)}` });
      expect(response.status()).toBe(400);
    });
  }

  test('rejects malformed JSON with 400', async ({ request }) => {
    const response = await request.post(ORDERS, { headers: { 'content-type': 'application/json' }, data: '{"customerName":' });
    test.info().annotations.push({ type: 'response', description: `${response.status()} ${(await response.text()).slice(0, 300)}` });
    expect(response.status()).toBe(400);
  });

  test('rejects a request with no body with 400', async ({ request }) => {
    const response = await request.post(ORDERS, { headers: { 'content-type': 'application/json' } });
    test.info().annotations.push({ type: 'response', description: `${response.status()} ${(await response.text()).slice(0, 300)}` });
    expect(response.status()).toBe(400);
  });

  test('validation errors come back as problem details', async ({ request }) => {
    const response = await request.post(ORDERS, { data: { customerName: '', pizzas: [] } });
    expect(response.headers()['content-type']).toContain('application/problem+json');
    const body = await response.json();
    expect(body.errors.CustomerName).toBeTruthy();
    expect(body.errors.Pizzas).toBeTruthy();
  });

  test('omitting toppingIds is a plain cheese pizza', async ({ request }) => {
    const response = await request.post(ORDERS, { data: { customerName: uniqueName('NoToppings'), pizzas: [{ sizeId: 2, quantity: 1 }] } });
    expect(response.status()).toBe(200);
    expect((await response.json()).total).toBe(14);
  });
});

test.describe('API: prices are decided by the server', () => {
  test('a client-supplied price or total is ignored', async ({ request }) => {
    const response = await request.post(ORDERS, {
      data: {
        customerName: uniqueName('Tamper'),
        total: 0.01,
        pizzas: [{ ...pizza({ sizeId: 2, toppingIds: [1, 2, 3, 4], quantity: 2 }), unitPrice: 0.01, lineTotal: 0.01 }],
      },
    });
    expect(response.status()).toBe(200);
    const order = await response.json();
    expect(order.total).toBe(44);
    expect(order.pizzas[0].unitPrice).toBe(22);
  });

  test('toppings are always listed in menu order, whatever order they were sent in', async ({ request }) => {
    const order = await (await request.post(ORDERS, { data: { customerName: uniqueName('Order'), pizzas: [pizza({ toppingIds: [4, 1]})] } })).json();
    expect(order.pizzas[0].toppings).toEqual(['Pepperoni', 'Green peppers']);
  });

  test('observation: largest order accepted', async ({ request }) => {
    const response = await request.post(ORDERS, {
      data: { customerName: uniqueName('Huge'), pizzas: Array.from({ length: 20 }, () => pizza({ sizeId: 2, toppingIds: [1, 2, 3, 4], quantity: 20 })) },
    });
    const order = await response.json();
    test.info().annotations.push({ type: 'observation', description: `20 lines x 20 Works Large accepted: status ${response.status()}, total $${order.total}, pizzas ${order.pizzas.reduce((n: number, p: any) => n + p.quantity, 0)}` });
    expect(response.status()).toBe(200);
  });
});

test.describe('API: order lookup and kitchen', () => {
  test('unknown order and malformed id', async ({ request }) => {
    expect((await request.get(`${ORDERS}/00000000-0000-0000-0000-000000000000`)).status()).toBe(404);
    expect((await request.get(`${ORDERS}/not-a-guid`)).status()).toBe(404);
  });

  test('marking an unknown order Ready is 404', async ({ request }) => {
    const response = await request.patch(`${API_URL}/api/v1/kitchen/orders/00000000-0000-0000-0000-000000000000/ready`);
    expect(response.status()).toBe(404);
  });

  test('marking Ready twice succeeds both times and keeps the first ReadyAt', async ({ request }) => {
    const order = await (await request.post(ORDERS, { data: { customerName: uniqueName('Twice'), pizzas: [pizza()] } })).json();
    const first = await request.patch(`${API_URL}/api/v1/kitchen/orders/${order.id}/ready`);
    const second = await request.patch(`${API_URL}/api/v1/kitchen/orders/${order.id}/ready`);
    expect(first.status()).toBe(200);
    expect(second.status()).toBe(200);
    expect((await second.json()).readyAt).toBe((await first.json()).readyAt);
  });

  test('ten orders placed at once get ten different, consecutive numbers', async ({ request }) => {
    const prefix = uniqueName('Burst');
    const responses = await Promise.all(
      Array.from({ length: 10 }, (_, i) => request.post(ORDERS, { data: { customerName: `${prefix}-${i}`, pizzas: [pizza()] } })),
    );
    const statuses = responses.map(r => r.status());
    expect(statuses.every(s => s === 200), `statuses ${statuses}`).toBeTruthy();
    const numbers = (await Promise.all(responses.map(r => r.json()))).map(o => o.orderNumber).sort((a, b) => a - b);
    expect(new Set(numbers).size).toBe(10);
    expect(numbers[9] - numbers[0]).toBe(9);
  });

  test('first order number is 1001 or higher', async ({ request }) => {
    const order = await (await request.post(ORDERS, { data: { customerName: uniqueName('Number'), pizzas: [pizza()] } })).json();
    expect(order.orderNumber).toBeGreaterThanOrEqual(1001);
  });

  test('observation: kitchen and order endpoints have no authentication', async ({ request }) => {
    const kitchen = await request.get(`${API_URL}/api/v1/kitchen/orders`);
    test.info().annotations.push({ type: 'observation', description: `GET kitchen/orders without credentials: ${kitchen.status()}` });
  });
});
