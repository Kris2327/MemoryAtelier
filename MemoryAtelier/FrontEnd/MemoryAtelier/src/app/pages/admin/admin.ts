import {
  AfterViewInit,
  Component,
  ElementRef,
  OnInit,
  ViewChild,
  computed,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ProductService } from '../../services/product/product';
import { CategoryService } from '../../services/category/category';
import { ImageService } from '../../services/images/images';
import { HeroImageService } from '../../services/hero-image/hero-image';
import { ContactMessagesService } from '../../services/contact-messages/contact-messages';
import { OrdersService } from '../../services/orders/orders';
import { AdminOrder, AdminOrderItem, Category, CategoryRef, ContactMessage, CreateProductDto, HeroImage, OrderStatus, Product, TrashedCategory, TrashedProduct } from '../../services/auth/auth-types';
import { I18nService } from '../../services/i18n/i18n';
import { SeoService } from '../../services/seo/seo';
import { environment } from '../../../environments/environment';

declare global {
  interface Window {
    Chart?: any;
  }
}

interface MostFavouritedProduct {
  name: string;
  favouriteCount: number;
}

interface LowStockProduct {
  id: string;
  name: string;
  nameEn: string | null;
  categories: CategoryRef[];
  stock: number;
  imageUrl: string | null;
}

interface DashboardStats {
  totalProducts: number;
  totalCategories: number;
  totalOrders: number;
  totalRevenue: number;
  newUsersThisWeek: number;
  mostFavouritedProduct: MostFavouritedProduct | null;
  lowStockProducts: LowStockProduct[];
}

interface RevenueByMonthItem {
  month: string;
  revenue: number;
}

interface SalesByCategoryItem {
  category: string;
  count: number;
}

interface NewUsersByWeekItem {
  week: string;
  count: number;
}

interface DashboardCharts {
  revenueByMonth: RevenueByMonthItem[];
  salesByCategory: SalesByCategoryItem[];
  newUsersByWeek: NewUsersByWeekItem[];
}

type AdminPanel = 'dashboard' | 'management' | 'hero' | 'contact' | 'orders' | 'trash';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin.html',
  styleUrl: './admin.css'
})
export class Admin implements OnInit, AfterViewInit {
  @ViewChild('revenueChart') revenueChartRef?: ElementRef<HTMLCanvasElement>;
  @ViewChild('categoryChart') categoryChartRef?: ElementRef<HTMLCanvasElement>;
  @ViewChild('usersChart') usersChartRef?: ElementRef<HTMLCanvasElement>;

  private readonly productService = inject(ProductService);
  private readonly categoryService = inject(CategoryService);
  private readonly imageService = inject(ImageService);
  private readonly heroImageService = inject(HeroImageService);
  private readonly contactMessagesService = inject(ContactMessagesService);
  private readonly ordersService = inject(OrdersService);
  private readonly seo = inject(SeoService);
  private readonly http = inject(HttpClient);
  readonly i18n = inject(I18nService);

  private readonly dashboardApi = `${environment.apiUrl}/dashboard`;
  private readonly productApi = `${environment.apiUrl}/Product`;

  products = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  groupedProducts = signal<{ category: string; categoryEn: string | null; items: Product[] }[]>([]);

  managementLoading = signal(false);
  dashboardLoading = signal(false);
  chartsLoading = signal(false);
  uploadingImage = signal(false);
  showModal = signal(false);
  editingProduct = signal<Product | null>(null);
  deleteConfirmId = signal<string | null>(null);
  successMsg = signal('');

  activePanel = signal<AdminPanel>('dashboard');
  sidebarOpen = signal(false);

  dashboardStats = signal<DashboardStats | null>(null);
  dashboardCharts = signal<DashboardCharts | null>(null);
  stockDrafts = signal<Record<string, number>>({});
  stockSavingId = signal<string | null>(null);

  newSection = { name: '', nameEn: '', parentId: '' };
  editingCategoryId = signal<string | null>(null);
  categoryDraft = { name: '', nameEn: '' };
  expandedCategoryIds = signal<Set<string>>(new Set());
  collapsedGroups = signal<Set<string>>(new Set());

  trashedProducts = signal<TrashedProduct[]>([]);
  trashedCategories = signal<TrashedCategory[]>([]);
  trashLoading = signal(false);

  heroImages = signal<HeroImage[]>([]);
  heroLoading = signal(false);
  heroUploading = signal(false);

  contactMessages = signal<ContactMessage[]>([]);
  contactLoading = signal(false);
  selectedContactMessage = signal<ContactMessage | null>(null);
  replyDraft = signal('');
  sendingReply = signal(false);

  readonly pendingContactCount = computed(() => this.contactMessages().filter(m => !m.isReplied).length);

  orders = signal<AdminOrder[]>([]);
  ordersLoading = signal(false);
  selectedOrder = signal<AdminOrder | null>(null);
  statusDraft = signal<OrderStatus>('Pending');
  cancellationNoteDraft = signal('');
  savingStatus = signal(false);
  shipmentDrafts = signal<Record<string, { firstShipmentSentAt: string; nextShipmentEstimatedAt: string; finalShipmentSentAt: string }>>({});
  savingItemId = signal<string | null>(null);

  readonly orderStatuses: OrderStatus[] = ['Pending', 'Shipped', 'Cancelled'];

  form: CreateProductDto = {
    name: '',
    nameEn: null,
    categoryIds: [],
    price: 0,
    description: '',
    descriptionEn: null,
    imageUrls: [],
    stock: 0
  };
  categoryDropdownOpen = signal(false);
  subcategoryDropdownOpen = signal(false);
  subSubDropdownOpen = signal(false);

  private revenueChart?: any;
  private categoryChart?: any;
  private usersChart?: any;
  private chartJsPromise?: Promise<void>;

  readonly statsCards = computed(() => {
    const stats = this.dashboardStats();
    const mostFavourited = stats?.mostFavouritedProduct;

    return [
      { label: this.i18n.t('admin.stat.totalProducts'), value: stats?.totalProducts ?? 0, accent: 'primary' },
      { label: this.i18n.t('admin.stat.totalCategories'), value: stats?.totalCategories ?? 0, accent: 'dark' },
      { label: this.i18n.t('admin.stat.totalOrders'), value: stats?.totalOrders ?? 0, accent: 'sand' },
      { label: this.i18n.t('admin.stat.totalRevenue'), value: this.formatCurrency(stats?.totalRevenue ?? 0), accent: 'primary' },
      { label: this.i18n.t('admin.stat.newUsers'), value: stats?.newUsersThisWeek ?? 0, accent: 'dark' },
      {
        label: this.i18n.t('admin.stat.mostFavourited'),
        value: mostFavourited ? mostFavourited.name : this.i18n.t('admin.stat.noData'),
        subValue: `${mostFavourited?.favouriteCount ?? 0} ${this.i18n.t('admin.stat.favouritesSuffix')}`,
        accent: 'sand'
      }
    ];
  });

  readonly totalProducts = computed(() => this.products().length);
  readonly totalCategories = computed(() => this.flatCategories().length);
  readonly lowStockCount = computed(() => this.dashboardStats()?.lowStockProducts.length ?? 0);
  readonly unseenOrdersCount = computed(() => this.orders().filter(o => !o.isSeen).length);

  ngOnInit(): void {
    this.seo.update({ title: 'Админ панел | Memory Atelier', description: 'Администраторски панел на Memory Atelier.', path: '/admin', noindex: true });
    this.loadManagementData();
    this.loadDashboardData();
    this.loadHeroImages();
    this.loadContactMessages();
    this.loadOrders();
  }

  ngAfterViewInit(): void {
    this.renderChartsSoon();
  }

  loadManagementData(): void {
    this.loadCategories();
    this.loadProducts();
  }

  loadCategories(): void {
    this.categoryService.getAll().subscribe({
      next: data => {
        this.categories.set(data);
        this.categoryService.categories.set(data);
      }
    });
  }

  loadProducts(): void {
    this.managementLoading.set(true);
    this.productService.getAll().subscribe({
      next: data => {
        this.products.set(data);
        this.groupProducts(data);
        this.managementLoading.set(false);
      },
      error: () => this.managementLoading.set(false)
    });
  }

  loadDashboardData(): void {
    this.dashboardLoading.set(true);
    this.chartsLoading.set(true);

    this.http.get<DashboardStats>(`${this.dashboardApi}/stats`).subscribe({
      next: data => {
        this.dashboardStats.set(data);
        this.stockDrafts.set(
          data.lowStockProducts.reduce<Record<string, number>>((drafts, product) => {
            drafts[product.id] = product.stock;
            return drafts;
          }, {})
        );
        this.dashboardLoading.set(false);
      },
      error: () => this.dashboardLoading.set(false)
    });

    this.http.get<DashboardCharts>(`${this.dashboardApi}/charts`).subscribe({
      next: async data => {
        this.dashboardCharts.set(data);
        this.chartsLoading.set(false);
        await this.ensureChartJs();
        this.renderChartsSoon();
      },
      error: () => this.chartsLoading.set(false)
    });
  }

  setActivePanel(panel: AdminPanel): void {
    this.activePanel.set(panel);
    this.sidebarOpen.set(false);

    if (panel === 'dashboard') {
      this.renderChartsSoon();
    }

    if (panel === 'orders') {
      this.loadOrders();
    }

    if (panel === 'trash') {
      this.loadTrash();
    }
  }

  panelEyebrow(): string {
    switch (this.activePanel()) {
      case 'dashboard': return this.i18n.t('admin.navDashboard');
      case 'hero': return this.i18n.t('admin.navHero');
      case 'contact': return this.i18n.t('admin.navContact');
      case 'orders': return this.i18n.t('admin.navOrders');
      case 'trash': return this.i18n.t('admin.navTrash');
      default: return this.i18n.t('admin.navManagement');
    }
  }

  panelTitle(): string {
    switch (this.activePanel()) {
      case 'dashboard': return this.i18n.t('admin.titleDashboard');
      case 'hero': return this.i18n.t('admin.titleHero');
      case 'contact': return this.i18n.t('admin.titleContact');
      case 'orders': return this.i18n.t('admin.titleOrders');
      case 'trash': return this.i18n.t('admin.titleTrash');
      default: return this.i18n.t('admin.titleManagement');
    }
  }

  panelSubtitle(): string {
    switch (this.activePanel()) {
      case 'dashboard': return this.i18n.t('admin.subtitleDashboard');
      case 'hero': return this.i18n.t('admin.subtitleHero');
      case 'contact': return this.i18n.t('admin.subtitleContact');
      case 'orders': return this.i18n.t('admin.subtitleOrders');
      case 'trash': return this.i18n.t('admin.subtitleTrash');
      default: return this.i18n.t('admin.subtitleManagement');
    }
  }

  toggleSidebar(): void {
    this.sidebarOpen.set(!this.sidebarOpen());
  }

  groupProducts(data: Product[]): void {
    const map = new Map<string, { categoryEn: string | null; items: Product[] }>();

    data.forEach(product => {
      const cats: CategoryRef[] = product.categories.length
        ? product.categories
        : [{ id: '', name: this.i18n.t('admin.noCategory'), nameEn: null }];

      cats.forEach(cat => {
        if (!map.has(cat.name)) {
          map.set(cat.name, { categoryEn: cat.nameEn, items: [] });
        }
        map.get(cat.name)!.items.push(product);
      });
    });

    this.groupedProducts.set(
      Array.from(map.entries()).map(([category, { categoryEn, items }]) => ({ category, categoryEn, items }))
    );
  }

  groupLabel(group: { category: string; categoryEn: string | null }): string {
    return this.i18n.pick(group.category, group.categoryEn);
  }

  categoryNames(categories: CategoryRef[]): string {
    return categories.length
      ? categories.map(c => this.i18n.pick(c.name, c.nameEn)).join(', ')
      : this.i18n.t('admin.noCategory');
  }

  flatCategories(): Category[] {
    const result: Category[] = [];
    const flatten = (categories: Category[]) => {
      categories.forEach(category => {
        result.push(category);
        flatten(category.children);
      });
    };

    flatten(this.categories());
    return result;
  }

  toggleCategorySelection(id: string): void {
    const idx = this.form.categoryIds.indexOf(id);
    if (idx >= 0) {
      this.form.categoryIds.splice(idx, 1);
    } else {
      this.form.categoryIds.push(id);
    }
  }

  isCategorySelected(id: string): boolean {
    return this.form.categoryIds.includes(id);
  }

  toggleCategoryDropdown(): void {
    this.categoryDropdownOpen.update(v => !v);
    this.subcategoryDropdownOpen.set(false);
    this.subSubDropdownOpen.set(false);
  }

  toggleSubcategoryDropdown(): void {
    if (!this.selectedSubcategories().length) return;
    this.subcategoryDropdownOpen.update(v => !v);
    this.categoryDropdownOpen.set(false);
    this.subSubDropdownOpen.set(false);
  }

  toggleSubSubDropdown(): void {
    this.subSubDropdownOpen.update(v => !v);
    this.categoryDropdownOpen.set(false);
    this.subcategoryDropdownOpen.set(false);
  }

  closeAllCategoryDropdowns(): void {
    this.categoryDropdownOpen.set(false);
    this.subcategoryDropdownOpen.set(false);
    this.subSubDropdownOpen.set(false);
  }

  categoryDropdownLabel(): string {
    const selected = this.categories().filter(c => this.isCategorySelected(c.id));
    return selected.length
      ? selected.map(c => this.i18n.pick(c.name, c.nameEn)).join(', ')
      : this.i18n.t('admin.modal.chooseCategory');
  }

  subcategoryDropdownLabel(): string {
    const selected = this.selectedSubcategories().filter(c => this.isCategorySelected(c.id));
    return selected.length
      ? selected.map(c => this.i18n.pick(c.name, c.nameEn)).join(', ')
      : this.i18n.t('admin.modal.subcategoryLabel');
  }

  grandchildDropdownLabel(): string {
    const selected = this.selectedGrandchildren().filter(c => this.isCategorySelected(c.id));
    return selected.length
      ? selected.map(c => this.i18n.pick(c.name, c.nameEn)).join(', ')
      : this.i18n.t('admin.modal.moreSubcategoriesLabel');
  }

  selectedGrandchildren(): Category[] {
    const result: Category[] = [];
    for (const sub of this.selectedSubcategories()) {
      if (this.isCategorySelected(sub.id)) {
        for (const child of sub.children) {
          if (!result.some(r => r.id === child.id)) {
            result.push(child);
          }
        }
      }
    }
    return result;
  }

  selectedSubcategories(): Category[] {
    const result: Category[] = [];
    for (const root of this.categories()) {
      if (this.isCategorySelected(root.id)) {
        for (const sub of root.children) {
          if (!result.some(r => r.id === sub.id)) {
            result.push(sub);
          }
        }
      }
    }
    return result;
  }

  openAdd(): void {
    this.editingProduct.set(null);
    this.form = {
      name: '',
      nameEn: null,
      categoryIds: [],
      price: 0,
      description: '',
      descriptionEn: null,
      imageUrls: [],
      stock: 0
    };
    this.closeAllCategoryDropdowns();
    this.showModal.set(true);
    document.body.style.overflow = 'hidden';
  }

  openEdit(product: Product): void {
    this.editingProduct.set(product);
    this.form = {
      name: product.name,
      nameEn: product.nameEn,
      categoryIds: product.categories.map(c => c.id),
      price: product.price,
      description: product.description,
      descriptionEn: product.descriptionEn,
      imageUrls: product.images.map(image => image.imageUrl),
      stock: product.stock
    };
    this.closeAllCategoryDropdowns();
    this.showModal.set(true);
    document.body.style.overflow = 'hidden';
  }

  closeModal(): void {
    this.showModal.set(false);
    this.editingProduct.set(null);
    document.body.style.overflow = '';
  }

  saveProduct(): void {
    const editing = this.editingProduct();

    if (editing) {
      this.productService.update(editing.id, this.form).subscribe({
        next: () => {
          this.loadProducts();
          this.loadDashboardData();
          this.closeModal();
          this.showSuccess(this.i18n.t('admin.toast.productUpdated'));
        }
      });
      return;
    }

    this.productService.create(this.form).subscribe({
      next: () => {
        this.loadProducts();
        this.loadDashboardData();
        this.closeModal();
        this.showSuccess(this.i18n.t('admin.toast.productAdded'));
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) {
      return;
    }

    const files = Array.from(input.files);
    this.uploadingImage.set(true);

    let completed = 0;
    files.forEach(file => {
      this.imageService.upload(file).subscribe({
        next: response => {
          this.form.imageUrls.push(response.url);
          completed++;
          if (completed === files.length) {
            this.uploadingImage.set(false);
          }
        },
        error: () => {
          completed++;
          if (completed === files.length) {
            this.uploadingImage.set(false);
          }
        }
      });
    });

    input.value = '';
  }

  removeImageUrl(index: number): void {
    this.form.imageUrls.splice(index, 1);
  }

  loadHeroImages(): void {
    this.heroLoading.set(true);
    this.heroImageService.getAll().subscribe({
      next: data => { this.heroImages.set(data); this.heroLoading.set(false); },
      error: () => this.heroLoading.set(false)
    });
  }

  onHeroFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) {
      return;
    }

    const files = Array.from(input.files);
    this.heroUploading.set(true);

    let completed = 0;
    files.forEach(file => {
      this.imageService.upload(file).subscribe({
        next: response => {
          this.heroImageService.create(response.url).subscribe({
            next: image => {
              this.heroImages.update(images => [...images, image]);
              completed++;
              if (completed === files.length) this.heroUploading.set(false);
            },
            error: () => {
              completed++;
              if (completed === files.length) this.heroUploading.set(false);
            }
          });
        },
        error: () => {
          completed++;
          if (completed === files.length) this.heroUploading.set(false);
        }
      });
    });

    input.value = '';
  }

  removeHeroImage(id: string): void {
    this.heroImageService.delete(id).subscribe({
      next: () => this.heroImages.update(images => images.filter(i => i.id !== id))
    });
  }

  moveHeroImage(index: number, direction: number): void {
    const images = [...this.heroImages()];
    const target = index + direction;
    if (target < 0 || target >= images.length) return;

    [images[index], images[target]] = [images[target], images[index]];
    this.heroImages.set(images);
    this.heroImageService.reorder(images.map(i => i.id)).subscribe();
  }

  loadContactMessages(): void {
    this.contactLoading.set(true);
    this.contactMessagesService.getAll().subscribe({
      next: data => { this.contactMessages.set(data); this.contactLoading.set(false); },
      error: () => this.contactLoading.set(false)
    });
  }

  openContactMessage(message: ContactMessage): void {
    this.selectedContactMessage.set(message);
    this.replyDraft.set(message.replyText ?? '');
    document.body.style.overflow = 'hidden';
  }

  closeContactMessage(): void {
    this.selectedContactMessage.set(null);
    this.replyDraft.set('');
    document.body.style.overflow = '';
  }

  sendReply(): void {
    const message = this.selectedContactMessage();
    const replyText = this.replyDraft().trim();
    if (!message || !replyText) {
      return;
    }

    this.sendingReply.set(true);
    this.contactMessagesService.reply(message.id, replyText).subscribe({
      next: () => {
        const repliedAt = new Date().toISOString();
        this.contactMessages.update(messages =>
          messages.map(m => m.id === message.id ? { ...m, isReplied: true, replyText, repliedAt } : m)
        );
        this.sendingReply.set(false);
        this.closeContactMessage();
        this.showSuccess(this.i18n.t('admin.contact.toastSent'));
      },
      error: () => this.sendingReply.set(false)
    });
  }

  trackByContactMessage(_: number, message: ContactMessage): string {
    return message.id;
  }

  loadOrders(): void {
    this.ordersLoading.set(true);
    this.ordersService.getAll().subscribe({
      next: data => { this.orders.set(data); this.ordersLoading.set(false); },
      error: () => this.ordersLoading.set(false)
    });
  }

  openOrderDetail(order: AdminOrder): void {
    this.selectedOrder.set(order);
    this.statusDraft.set(order.status);
    this.cancellationNoteDraft.set(order.cancellationNote ?? '');
    this.shipmentDrafts.set(
      order.items.reduce<Record<string, { firstShipmentSentAt: string; nextShipmentEstimatedAt: string; finalShipmentSentAt: string }>>((drafts, item) => {
        drafts[item.id] = {
          firstShipmentSentAt: item.firstShipmentSentAt ? item.firstShipmentSentAt.slice(0, 10) : '',
          nextShipmentEstimatedAt: item.nextShipmentEstimatedAt ? item.nextShipmentEstimatedAt.slice(0, 10) : '',
          finalShipmentSentAt: item.finalShipmentSentAt ? item.finalShipmentSentAt.slice(0, 10) : ''
        };
        return drafts;
      }, {})
    );
    document.body.style.overflow = 'hidden';

    if (!order.isSeen) {
      this.ordersService.markSeen(order.id).subscribe({
        next: () => {
          this.orders.update(orders => orders.map(o => o.id === order.id ? { ...o, isSeen: true } : o));
          this.selectedOrder.update(current => current ? { ...current, isSeen: true } : current);
        }
      });
    }
  }

  closeOrderDetail(): void {
    this.selectedOrder.set(null);
    document.body.style.overflow = '';
  }

  saveOrderStatus(): void {
    const order = this.selectedOrder();
    if (!order) return;

    const status = this.statusDraft();
    const cancellationNote = status === 'Cancelled' ? (this.cancellationNoteDraft().trim() || null) : order.cancellationNote;

    this.savingStatus.set(true);
    this.ordersService.updateStatus(order.id, status, status === 'Cancelled' ? cancellationNote : null).subscribe({
      next: () => {
        this.orders.update(orders => orders.map(o => o.id === order.id ? { ...o, status, cancellationNote } : o));
        this.selectedOrder.update(current => current ? { ...current, status, cancellationNote } : current);
        this.savingStatus.set(false);
        this.showSuccess(this.i18n.t('admin.orders.toastStatusUpdated'));
      },
      error: () => this.savingStatus.set(false)
    });
  }

  updateShipmentDraft(itemId: string, field: 'firstShipmentSentAt' | 'nextShipmentEstimatedAt' | 'finalShipmentSentAt', value: string): void {
    this.shipmentDrafts.update(drafts => ({
      ...drafts,
      [itemId]: { ...drafts[itemId], [field]: value }
    }));
  }

  hasPendingFulfillmentItems(order: AdminOrder): boolean {
    return order.items.some(i => !!i.fulfillmentChoice && !i.finalShipmentSentAt);
  }

  availableStatusOptions(order: AdminOrder): OrderStatus[] {
    const base: OrderStatus[] = this.hasPendingFulfillmentItems(order)
      ? ['Pending', 'Cancelled']
      : this.orderStatuses;
    return base.includes(order.status) ? base : [...base, order.status];
  }

  saveShipmentInfo(item: AdminOrderItem): void {
    const order = this.selectedOrder();
    const draft = this.shipmentDrafts()[item.id];
    if (!order || !draft) return;

    this.savingItemId.set(item.id);
    const firstShipmentSentAt = draft.firstShipmentSentAt || null;
    const nextShipmentEstimatedAt = draft.nextShipmentEstimatedAt || null;
    const finalShipmentSentAt = draft.finalShipmentSentAt || null;

    this.ordersService.updateShipmentInfo(order.id, item.id, firstShipmentSentAt, nextShipmentEstimatedAt, finalShipmentSentAt).subscribe({
      next: () => {
        const updateItem = (i: AdminOrderItem) => i.id === item.id ? { ...i, firstShipmentSentAt, nextShipmentEstimatedAt, finalShipmentSentAt } : i;

        const applyUpdate = (o: AdminOrder): AdminOrder => {
          const items = o.items.map(updateItem);
          const nowFullyShipped = !items.some(i => !!i.fulfillmentChoice && !i.finalShipmentSentAt);
          const status: OrderStatus = nowFullyShipped && o.status !== 'Cancelled' ? 'Shipped' : o.status;
          return { ...o, items, status };
        };

        this.orders.update(orders => orders.map(o => o.id === order.id ? applyUpdate(o) : o));
        this.selectedOrder.update(current => current ? applyUpdate(current) : current);
        this.statusDraft.set(applyUpdate(order).status);
        this.savingItemId.set(null);
        this.showSuccess(this.i18n.t('admin.orders.toastShipmentUpdated'));
      },
      error: () => this.savingItemId.set(null)
    });
  }

  orderStatusLabel(status: OrderStatus): string {
    switch (status) {
      case 'Pending': return this.i18n.t('admin.orders.statusPending');
      case 'Shipped': return this.i18n.t('admin.orders.statusShipped');
      case 'Delivered': return this.i18n.t('admin.orders.statusDelivered');
      case 'Cancelled': return this.i18n.t('admin.orders.statusCancelled');
    }
  }

  itemFulfillmentLabel(item: AdminOrderItem): string {
    if (item.fulfillmentChoice === 'Split') return this.i18n.t('admin.orders.fulfillmentSplit');
    if (item.fulfillmentChoice === 'Wait') return this.i18n.t('admin.orders.fulfillmentWait');
    return this.i18n.t('admin.orders.fulfillmentInStock');
  }

  itemFulfillmentClass(item: AdminOrderItem): string {
    if (!item.fulfillmentChoice) return 'fulfillment-instock';
    return item.finalShipmentSentAt ? 'fulfillment-resolved' : `fulfillment-${item.fulfillmentChoice.toLowerCase()}`;
  }

  orderFulfillmentTags(order: AdminOrder): { label: string; cls: string }[] {
    const tags: { label: string; cls: string }[] = [];
    const splitItems = order.items.filter(i => i.fulfillmentChoice === 'Split');
    const waitItems = order.items.filter(i => i.fulfillmentChoice === 'Wait');

    if (splitItems.length) {
      const pending = splitItems.some(i => !i.finalShipmentSentAt);
      tags.push({ label: this.i18n.t('admin.orders.fulfillmentSplit'), cls: pending ? 'fulfillment-split' : 'fulfillment-resolved' });
    }
    if (waitItems.length) {
      const pending = waitItems.some(i => !i.finalShipmentSentAt);
      tags.push({ label: this.i18n.t('admin.orders.fulfillmentWait'), cls: pending ? 'fulfillment-wait' : 'fulfillment-resolved' });
    }

    return tags;
  }

  trackByOrder(_: number, order: AdminOrder): string {
    return order.id;
  }

  trackByOrderItem(_: number, item: AdminOrderItem): string {
    return item.id;
  }

  confirmDelete(id: string): void {
    this.deleteConfirmId.set(id);
  }

  cancelDelete(): void {
    this.deleteConfirmId.set(null);
  }

  deleteProduct(id: string): void {
    this.productService.delete(id).subscribe({
      next: () => {
        this.loadProducts();
        this.loadDashboardData();
        this.deleteConfirmId.set(null);
        this.showSuccess(this.i18n.t('admin.toast.productDeleted'));
      }
    });
  }

  addSection(): void {
    if (!this.newSection.name.trim()) {
      return;
    }

    this.categoryService.create({
      name: this.newSection.name,
      nameEn: this.newSection.nameEn.trim() || null,
      parentId: this.newSection.parentId || null
    }).subscribe({
      next: () => {
        this.loadCategories();
        this.loadDashboardData();
        this.showSuccess(`${this.i18n.t('admin.toast.sectionAddedPrefix')} "${this.newSection.name}" ${this.i18n.t('admin.toast.sectionAddedSuffix')}`);
        this.newSection = { name: '', nameEn: '', parentId: '' };
      }
    });
  }

  deleteSection(id: string): void {
    this.categoryService.delete(id).subscribe({
      next: () => {
        this.loadCategories();
        this.loadDashboardData();
        this.showSuccess(this.i18n.t('admin.toast.sectionDeleted'));
      }
    });
  }

  loadTrash(): void {
    this.trashLoading.set(true);
    this.productService.getDeleted().subscribe({
      next: data => { this.trashedProducts.set(data); this.trashLoading.set(false); },
      error: () => this.trashLoading.set(false)
    });
    this.categoryService.getDeleted().subscribe({
      next: data => this.trashedCategories.set(data)
    });
  }

  restoreProduct(id: string): void {
    this.productService.restore(id).subscribe({
      next: () => {
        this.loadTrash();
        this.loadProducts();
        this.loadDashboardData();
        this.showSuccess(this.i18n.t('admin.toast.productRestored'));
      }
    });
  }

  restoreCategory(id: string): void {
    this.categoryService.restore(id).subscribe({
      next: () => {
        this.loadTrash();
        this.loadCategories();
        this.loadDashboardData();
        this.showSuccess(this.i18n.t('admin.toast.sectionRestored'));
      }
    });
  }

  startEditCategory(cat: Category): void {
    this.editingCategoryId.set(cat.id);
    this.categoryDraft = { name: cat.name, nameEn: cat.nameEn ?? '' };
  }

  cancelEditCategory(): void {
    this.editingCategoryId.set(null);
  }

  toggleCategoryExpand(id: string): void {
    this.expandedCategoryIds.update(ids => {
      const next = new Set(ids);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }

  isCategoryExpanded(id: string): boolean {
    return this.expandedCategoryIds().has(id);
  }

  toggleGroupCollapse(category: string): void {
    this.collapsedGroups.update(categories => {
      const next = new Set(categories);
      if (next.has(category)) {
        next.delete(category);
      } else {
        next.add(category);
      }
      return next;
    });
  }

  isGroupCollapsed(category: string): boolean {
    return this.collapsedGroups().has(category);
  }

  saveEditCategory(id: string): void {
    if (!this.categoryDraft.name.trim()) {
      return;
    }

    this.categoryService.update(id, {
      name: this.categoryDraft.name,
      nameEn: this.categoryDraft.nameEn.trim() || null
    }).subscribe({
      next: () => {
        this.loadCategories();
        this.editingCategoryId.set(null);
        this.showSuccess(this.i18n.t('admin.toast.sectionUpdated'));
      }
    });
  }

  updateStockDraft(productId: string, value: string | number): void {
    const parsed = Number(value);
    if (Number.isNaN(parsed)) {
      return;
    }

    this.stockDrafts.update(drafts => ({
      ...drafts,
      [productId]: parsed
    }));
  }

  saveStock(product: LowStockProduct): void {
    const nextStock = Math.max(0, Math.trunc(this.stockDrafts()[product.id] ?? product.stock));
    this.stockSavingId.set(product.id);

    this.http.patch<Product>(`${this.productApi}/${product.id}/stock`, { stock: nextStock }).subscribe({
      next: () => {
        this.stockSavingId.set(null);
        this.loadProducts();
        this.loadDashboardData();
        this.showSuccess(`${this.i18n.t('admin.toast.stockUpdatedPrefix')} "${product.name}" ${this.i18n.t('admin.toast.stockUpdatedSuffix')}`);
      },
      error: () => this.stockSavingId.set(null)
    });
  }

  getStockDraft(productId: string, fallback: number): number {
    return this.stockDrafts()[productId] ?? fallback;
  }

  showSuccess(message: string): void {
    this.successMsg.set(message);
    window.setTimeout(() => this.successMsg.set(''), 3000);
  }

  trackByProduct(_: number, product: Product): string {
    return product.id;
  }

  trackByLowStock(_: number, product: LowStockProduct): string {
    return product.id;
  }

  trackByCategory(_: number, category: Category): string {
    return category.id;
  }

  truncate(text: string, maxLength: number = 200): string {
    return text.length > maxLength ? text.slice(0, maxLength).trimEnd() + '…' : text;
  }

  private async ensureChartJs(): Promise<void> {
    if (window.Chart) {
      return;
    }

    if (!this.chartJsPromise) {
      this.chartJsPromise = new Promise<void>((resolve, reject) => {
        const existingScript = document.querySelector<HTMLScriptElement>(
          'script[data-chartjs="memory-atelier"]'
        );

        if (existingScript) {
          existingScript.addEventListener('load', () => resolve(), { once: true });
          existingScript.addEventListener('error', () => reject(), { once: true });
          return;
        }

        const script = document.createElement('script');
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/Chart.js/4.4.1/chart.umd.min.js';
        script.async = true;
        script.dataset['chartjs'] = 'memory-atelier';
        script.onload = () => resolve();
        script.onerror = () => reject(new Error('Chart.js failed to load.'));
        document.head.appendChild(script);
      });
    }

    await this.chartJsPromise;
  }

  private renderChartsSoon(): void {
    window.setTimeout(() => this.renderCharts(), 0);
  }

  private renderCharts(): void {
    const chartData = this.dashboardCharts();
    if (!chartData || this.activePanel() !== 'dashboard' || !window.Chart) {
      return;
    }

    const revenueCanvas = this.revenueChartRef?.nativeElement;
    const categoryCanvas = this.categoryChartRef?.nativeElement;
    const usersCanvas = this.usersChartRef?.nativeElement;

    if (!revenueCanvas || !categoryCanvas || !usersCanvas) {
      return;
    }

    this.revenueChart?.destroy();
    this.categoryChart?.destroy();
    this.usersChart?.destroy();

    const palette = ['#c97d4e', '#2c1810', '#f5c4a1', '#e8a87c', '#8b4513'];

    this.revenueChart = new window.Chart(revenueCanvas, {
      type: 'line',
      data: {
        labels: chartData.revenueByMonth.map(item => item.month),
        datasets: [
          {
            label: this.i18n.t('admin.chart.revenueLabel'),
            data: chartData.revenueByMonth.map(item => item.revenue),
            borderColor: '#c97d4e',
            backgroundColor: 'rgba(201, 125, 78, 0.18)',
            fill: true,
            tension: 0.35,
            pointRadius: 4,
            pointHoverRadius: 5
          }
        ]
      },
      options: this.buildChartOptions('€')
    });

    this.categoryChart = new window.Chart(categoryCanvas, {
      type: 'doughnut',
      data: {
        labels: chartData.salesByCategory.map(item => item.category),
        datasets: [
          {
            data: chartData.salesByCategory.map(item => item.count),
            backgroundColor: palette,
            borderColor: '#fffaf6',
            borderWidth: 4
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'bottom',
            labels: {
              color: '#5e4639',
              padding: 18,
              font: {
                family: 'Lato',
                size: 12
              }
            }
          }
        }
      }
    });

    this.usersChart = new window.Chart(usersCanvas, {
      type: 'bar',
      data: {
        labels: chartData.newUsersByWeek.map(item => item.week),
        datasets: [
          {
            label: this.i18n.t('admin.chart.usersTitle'),
            data: chartData.newUsersByWeek.map(item => item.count),
            backgroundColor: ['#c97d4e', '#2c1810', '#f5c4a1', '#e8a87c', '#8b4513', '#c97d4e'],
            borderRadius: 12,
            maxBarThickness: 44
          }
        ]
      },
      options: this.buildChartOptions()
    });
  }

  private buildChartOptions(ySuffix = ''): Record<string, unknown> {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: false
        }
      },
      scales: {
        x: {
          grid: {
            display: false
          },
          ticks: {
            color: '#5e4639',
            font: {
              family: 'Lato',
              size: 12
            }
          }
        },
        y: {
          beginAtZero: true,
          grid: {
            color: 'rgba(44, 24, 16, 0.08)'
          },
          ticks: {
            color: '#5e4639',
            callback: (value: string | number) => `${value}${ySuffix ? ` ${ySuffix}` : ''}`,
            font: {
              family: 'Lato',
              size: 12
            }
          }
        }
      }
    };
  }

  private formatCurrency(value: number): string {
    return new Intl.NumberFormat('bg-BG', {
      style: 'currency',
      currency: 'EUR',
      maximumFractionDigits: 2
    }).format(value);
  }
}
