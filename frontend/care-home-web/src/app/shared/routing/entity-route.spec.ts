import { entityRouteKey, isUuidRouteKey } from './entity-route';

describe('entityRouteKey', () => {
  it('prefers publicId over numeric id', () => {
    const key = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';
    expect(entityRouteKey({ id: 42, publicId: key })).toBe(key);
  });

  it('falls back to string id when publicId is missing', () => {
    expect(entityRouteKey({ id: 7 })).toBe('7');
  });
});

describe('isUuidRouteKey', () => {
  it('recognises GUID route keys', () => {
    expect(isUuidRouteKey('aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee')).toBe(true);
  });

  it('rejects numeric keys', () => {
    expect(isUuidRouteKey('123')).toBe(false);
  });
});
