import { TestBed } from '@angular/core/testing';

import { Images } from './images';

describe('Images', () => {
  let service: Images;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Images);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
