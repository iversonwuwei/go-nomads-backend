-- ============================================
-- Go Nomads 完整数据库架构 (Supabase/PostgreSQL + PostGIS)
-- 创建日期: 2025-10-22
-- 版本: 1.0.0
-- ============================================

-- 启用 PostGIS 扩展
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================
-- 1. 用户服务 (UserService)
-- ============================================

-- 角色表
CREATE TABLE IF NOT EXISTS public.roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    permissions JSONB DEFAULT '[]',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 用户表
CREATE TABLE IF NOT EXISTS public.users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    phone VARCHAR(50) UNIQUE,
    email VARCHAR(200) UNIQUE NOT NULL,
    password_hash VARCHAR(255),
    name VARCHAR(200) NOT NULL,
    nickname VARCHAR(100),
    avatar TEXT,
    bio TEXT,
    city VARCHAR(100),
    country VARCHAR(100),
    occupation VARCHAR(100),
    skills TEXT[],
    interests TEXT[],
    role_id UUID REFERENCES public.roles(id),
    is_active BOOLEAN DEFAULT true,
    is_verified BOOLEAN DEFAULT false,
    last_login_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 插入默认角色
INSERT INTO public.roles (name, description, permissions) VALUES 
    ('user', '普通用户', '["read"]'),
    ('admin', '管理员', '["read", "write", "delete"]'),
    ('moderator', '版主', '["read", "write"]')
ON CONFLICT (name) DO NOTHING;

-- 用户表索引
CREATE INDEX IF NOT EXISTS idx_users_email ON public.users(email);
CREATE INDEX IF NOT EXISTS idx_users_phone ON public.users(phone);
CREATE INDEX IF NOT EXISTS idx_users_role_id ON public.users(role_id);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON public.users(is_active);

-- ============================================
-- 2. 城市服务 (CityService)
-- ============================================

CREATE TABLE IF NOT EXISTS public.cities (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    country VARCHAR(100) NOT NULL,
    region VARCHAR(100),
    climate VARCHAR(50),
    description TEXT,
    image_url TEXT,
    weather VARCHAR(50),
    temperature DECIMAL(5,2),
    cost_of_living DECIMAL(10,2),
    internet_speed DECIMAL(10,2),
    safety_score DECIMAL(3,2),
    overall_score DECIMAL(3,2),
    fun_score DECIMAL(3,2),
    quality_of_life DECIMAL(3,2),
    internet_quality_score DECIMAL(3,2),
    cost_score DECIMAL(3,2),
    community_score DECIMAL(3,2),
    weather_score DECIMAL(3,2),
    aqi INTEGER,
    population INTEGER,
    timezone VARCHAR(50),
    currency VARCHAR(10),
    humidity INTEGER,
    location GEOGRAPHY(POINT, 4326),
    latitude DECIMAL(10,8),
    longitude DECIMAL(11,8),
    tags TEXT[],
    is_active BOOLEAN DEFAULT true,
    created_by UUID REFERENCES public.users(id),
    updated_by UUID REFERENCES public.users(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 城市表索引
CREATE INDEX IF NOT EXISTS idx_cities_name ON public.cities(name);
CREATE INDEX IF NOT EXISTS idx_cities_country ON public.cities(country);
CREATE INDEX IF NOT EXISTS idx_cities_location ON public.cities USING GIST(location);
CREATE INDEX IF NOT EXISTS idx_cities_overall_score ON public.cities(overall_score DESC);
CREATE INDEX IF NOT EXISTS idx_cities_is_active ON public.cities(is_active);
CREATE INDEX IF NOT EXISTS idx_cities_tags ON public.cities USING GIN(tags);

-- ============================================
-- 3. 共享办公服务 (CoworkingService)
-- ============================================

CREATE TABLE IF NOT EXISTS public.coworking_spaces (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) NOT NULL,
    city_id UUID REFERENCES public.cities(id) ON DELETE CASCADE,
    address TEXT NOT NULL,
    description TEXT,
    image_url TEXT,
    images TEXT[],
    price_per_day DECIMAL(10,2),
    price_per_month DECIMAL(10,2),
    price_per_hour DECIMAL(10,2),
    currency VARCHAR(10) DEFAULT 'USD',
    rating DECIMAL(3,2) DEFAULT 0.0,
    review_count INTEGER DEFAULT 0,
    wifi_speed DECIMAL(10,2),
    has_meeting_room BOOLEAN DEFAULT false,
    has_coffee BOOLEAN DEFAULT false,
    has_parking BOOLEAN DEFAULT false,
    has_24_7_access BOOLEAN DEFAULT false,
    amenities TEXT[],
    capacity INTEGER,
    location GEOGRAPHY(POINT, 4326),
    latitude DECIMAL(10,8),
    longitude DECIMAL(11,8),
    phone VARCHAR(50),
    email VARCHAR(200),
    website TEXT,
    opening_hours JSONB,
    is_active BOOLEAN DEFAULT true,
    created_by UUID REFERENCES public.users(id),
    updated_by UUID REFERENCES public.users(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 共享办公空间预订表
CREATE TABLE IF NOT EXISTS public.coworking_bookings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    coworking_id UUID REFERENCES public.coworking_spaces(id) ON DELETE CASCADE,
    user_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    booking_date DATE NOT NULL,
    start_time TIME,
    end_time TIME,
    booking_type VARCHAR(20) CHECK (booking_type IN ('hourly', 'daily', 'monthly')),
    total_price DECIMAL(10,2) NOT NULL,
    currency VARCHAR(10) DEFAULT 'USD',
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'confirmed', 'cancelled', 'completed')),
    special_requests TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 共享办公空间索引
CREATE INDEX IF NOT EXISTS idx_coworking_city_id ON public.coworking_spaces(city_id);
CREATE INDEX IF NOT EXISTS idx_coworking_rating ON public.coworking_spaces(rating DESC);
CREATE INDEX IF NOT EXISTS idx_coworking_location ON public.coworking_spaces USING GIST(location);
CREATE INDEX IF NOT EXISTS idx_coworking_is_active ON public.coworking_spaces(is_active);
CREATE INDEX IF NOT EXISTS idx_coworking_bookings_user_id ON public.coworking_bookings(user_id);
CREATE INDEX IF NOT EXISTS idx_coworking_bookings_status ON public.coworking_bookings(status);

-- ============================================
-- 4. 住宿服务 (AccommodationService)
-- ============================================

CREATE TABLE IF NOT EXISTS public.hotels (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) NOT NULL,
    city_id UUID REFERENCES public.cities(id) ON DELETE CASCADE,
    address TEXT NOT NULL,
    location GEOGRAPHY(POINT, 4326),
    latitude DECIMAL(10,8),
    longitude DECIMAL(11,8),
    rating DECIMAL(3,2) DEFAULT 0.0,
    review_count INTEGER DEFAULT 0,
    description TEXT,
    amenities TEXT[],
    images TEXT[],
    category VARCHAR(50) DEFAULT 'mid-range' CHECK (category IN ('budget', 'mid-range', 'luxury', 'boutique')),
    star_rating INTEGER CHECK (star_rating >= 1 AND star_rating <= 5),
    price_per_night DECIMAL(10,2) DEFAULT 0.0,
    currency VARCHAR(10) DEFAULT 'USD',
    is_featured BOOLEAN DEFAULT false,
    phone VARCHAR(50),
    email VARCHAR(200),
    website TEXT,
    check_in_time TIME DEFAULT '14:00',
    check_out_time TIME DEFAULT '11:00',
    cancellation_policy TEXT,
    is_active BOOLEAN DEFAULT true,
    created_by UUID REFERENCES public.users(id),
    updated_by UUID REFERENCES public.users(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 房型表
CREATE TABLE IF NOT EXISTS public.room_types (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    hotel_id UUID REFERENCES public.hotels(id) ON DELETE CASCADE,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    max_occupancy INTEGER DEFAULT 2,
    size DECIMAL(6,2) DEFAULT 25.0,
    bed_type VARCHAR(50) DEFAULT 'Queen',
    price_per_night DECIMAL(10,2) NOT NULL,
    currency VARCHAR(10) DEFAULT 'USD',
    available_rooms INTEGER DEFAULT 0,
    amenities TEXT[],
    images TEXT[],
    is_available BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 酒店预订表
CREATE TABLE IF NOT EXISTS public.hotel_bookings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    hotel_id UUID REFERENCES public.hotels(id) ON DELETE CASCADE,
    room_type_id UUID REFERENCES public.room_types(id) ON DELETE CASCADE,
    user_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    check_in_date DATE NOT NULL,
    check_out_date DATE NOT NULL,
    number_of_rooms INTEGER DEFAULT 1,
    number_of_guests INTEGER DEFAULT 1,
    total_price DECIMAL(10,2) NOT NULL,
    currency VARCHAR(10) DEFAULT 'USD',
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'confirmed', 'cancelled', 'completed', 'no-show')),
    payment_status VARCHAR(20) DEFAULT 'pending' CHECK (payment_status IN ('pending', 'paid', 'refunded')),
    special_requests TEXT,
    guest_name VARCHAR(200),
    guest_email VARCHAR(200),
    guest_phone VARCHAR(50),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 住宿服务索引
CREATE INDEX IF NOT EXISTS idx_hotels_city_id ON public.hotels(city_id);
CREATE INDEX IF NOT EXISTS idx_hotels_is_featured ON public.hotels(is_featured);
CREATE INDEX IF NOT EXISTS idx_hotels_rating ON public.hotels(rating DESC);
CREATE INDEX IF NOT EXISTS idx_hotels_category ON public.hotels(category);
CREATE INDEX IF NOT EXISTS idx_hotels_location ON public.hotels USING GIST(location);
CREATE INDEX IF NOT EXISTS idx_room_types_hotel_id ON public.room_types(hotel_id);
CREATE INDEX IF NOT EXISTS idx_room_types_is_available ON public.room_types(is_available);
CREATE INDEX IF NOT EXISTS idx_hotel_bookings_user_id ON public.hotel_bookings(user_id);
CREATE INDEX IF NOT EXISTS idx_hotel_bookings_status ON public.hotel_bookings(status);
CREATE INDEX IF NOT EXISTS idx_hotel_bookings_check_in ON public.hotel_bookings(check_in_date);

-- ============================================
-- 5. 活动服务 (EventService)
-- ============================================

CREATE TABLE IF NOT EXISTS public.events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(200) NOT NULL,
    description TEXT,
    organizer_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    city_id UUID REFERENCES public.cities(id) ON DELETE CASCADE,
    location VARCHAR(200),
    address TEXT,
    image_url TEXT,
    images TEXT[],
    category VARCHAR(50) CHECK (category IN ('networking', 'workshop', 'social', 'sports', 'cultural', 'tech', 'business', 'other')),
    start_time TIMESTAMP WITH TIME ZONE NOT NULL,
    end_time TIMESTAMP WITH TIME ZONE,
    max_participants INTEGER,
    current_participants INTEGER DEFAULT 0,
    price DECIMAL(10,2) DEFAULT 0.0,
    currency VARCHAR(10) DEFAULT 'USD',
    status VARCHAR(20) DEFAULT 'upcoming' CHECK (status IN ('upcoming', 'ongoing', 'completed', 'cancelled')),
    location_type VARCHAR(20) DEFAULT 'physical' CHECK (location_type IN ('physical', 'online', 'hybrid')),
    meeting_link TEXT,
    latitude DECIMAL(10,8),
    longitude DECIMAL(11,8),
    tags TEXT[],
    is_featured BOOLEAN DEFAULT false,
    created_by UUID REFERENCES public.users(id),
    updated_by UUID REFERENCES public.users(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 活动参与者表
CREATE TABLE IF NOT EXISTS public.event_participants (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_id UUID REFERENCES public.events(id) ON DELETE CASCADE,
    user_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    status VARCHAR(20) DEFAULT 'registered' CHECK (status IN ('registered', 'attended', 'cancelled', 'no-show')),
    payment_status VARCHAR(20) DEFAULT 'pending' CHECK (payment_status IN ('pending', 'paid', 'refunded')),
    registered_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(event_id, user_id)
);

-- 活动服务索引
CREATE INDEX IF NOT EXISTS idx_events_city_id ON public.events(city_id);
CREATE INDEX IF NOT EXISTS idx_events_organizer_id ON public.events(organizer_id);
CREATE INDEX IF NOT EXISTS idx_events_status ON public.events(status);
CREATE INDEX IF NOT EXISTS idx_events_start_time ON public.events(start_time);
CREATE INDEX IF NOT EXISTS idx_events_category ON public.events(category);
CREATE INDEX IF NOT EXISTS idx_events_is_featured ON public.events(is_featured);
CREATE INDEX IF NOT EXISTS idx_event_participants_user_id ON public.event_participants(user_id);
CREATE INDEX IF NOT EXISTS idx_event_participants_event_id ON public.event_participants(event_id);

-- ============================================
-- 6. 创新项目服务 (InnovationService)
-- ============================================

CREATE TABLE IF NOT EXISTS public.innovations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    creator_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    category VARCHAR(50) CHECK (category IN ('tech', 'business', 'social', 'environment', 'education', 'health', 'other')),
    stage VARCHAR(50) DEFAULT 'idea' CHECK (stage IN ('idea', 'prototype', 'mvp', 'launched', 'scaling')),
    tags TEXT[],
    image_url TEXT,
    images TEXT[],
    video_url TEXT,
    demo_url TEXT,
    github_url TEXT,
    website_url TEXT,
    team_size INTEGER DEFAULT 1,
    looking_for TEXT[],
    skills_needed TEXT[],
    like_count INTEGER DEFAULT 0,
    view_count INTEGER DEFAULT 0,
    comment_count INTEGER DEFAULT 0,
    is_featured BOOLEAN DEFAULT false,
    is_public BOOLEAN DEFAULT true,
    created_by UUID REFERENCES public.users(id),
    updated_by UUID REFERENCES public.users(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 创新项目点赞表
CREATE TABLE IF NOT EXISTS public.innovation_likes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    innovation_id UUID REFERENCES public.innovations(id) ON DELETE CASCADE,
    user_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(innovation_id, user_id)
);

-- 创新项目评论表
CREATE TABLE IF NOT EXISTS public.innovation_comments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    innovation_id UUID REFERENCES public.innovations(id) ON DELETE CASCADE,
    user_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    parent_id UUID REFERENCES public.innovation_comments(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 创新项目服务索引
CREATE INDEX IF NOT EXISTS idx_innovations_creator_id ON public.innovations(creator_id);
CREATE INDEX IF NOT EXISTS idx_innovations_category ON public.innovations(category);
CREATE INDEX IF NOT EXISTS idx_innovations_stage ON public.innovations(stage);
CREATE INDEX IF NOT EXISTS idx_innovations_is_featured ON public.innovations(is_featured);
CREATE INDEX IF NOT EXISTS idx_innovations_like_count ON public.innovations(like_count DESC);
CREATE INDEX IF NOT EXISTS idx_innovation_likes_user_id ON public.innovation_likes(user_id);
CREATE INDEX IF NOT EXISTS idx_innovation_comments_innovation_id ON public.innovation_comments(innovation_id);

-- ============================================
-- 7. 旅行规划服务 (TravelPlanningService)
-- ============================================

CREATE TABLE IF NOT EXISTS public.travel_plans (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    cities UUID[] DEFAULT '{}',
    budget DECIMAL(10,2),
    currency VARCHAR(10) DEFAULT 'USD',
    status VARCHAR(20) DEFAULT 'planning' CHECK (status IN ('planning', 'booked', 'ongoing', 'completed', 'cancelled')),
    is_public BOOLEAN DEFAULT false,
    itinerary JSONB,
    notes TEXT,
    created_by UUID REFERENCES public.users(id),
    updated_by UUID REFERENCES public.users(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 旅行计划协作者表
CREATE TABLE IF NOT EXISTS public.travel_plan_collaborators (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    travel_plan_id UUID REFERENCES public.travel_plans(id) ON DELETE CASCADE,
    user_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    role VARCHAR(20) DEFAULT 'viewer' CHECK (role IN ('owner', 'editor', 'viewer')),
    added_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(travel_plan_id, user_id)
);

-- 旅行规划服务索引
CREATE INDEX IF NOT EXISTS idx_travel_plans_user_id ON public.travel_plans(user_id);
CREATE INDEX IF NOT EXISTS idx_travel_plans_status ON public.travel_plans(status);
CREATE INDEX IF NOT EXISTS idx_travel_plans_start_date ON public.travel_plans(start_date);
CREATE INDEX IF NOT EXISTS idx_travel_plan_collaborators_user_id ON public.travel_plan_collaborators(user_id);

-- ============================================
-- 8. 电商服务 (EcommerceService)
-- ============================================

CREATE TABLE IF NOT EXISTS public.products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) NOT NULL,
    description TEXT,
    category VARCHAR(50) CHECK (category IN ('gear', 'accessories', 'books', 'courses', 'services', 'other')),
    price DECIMAL(10,2) NOT NULL,
    currency VARCHAR(10) DEFAULT 'USD',
    stock INTEGER DEFAULT 0,
    images TEXT[],
    tags TEXT[],
    rating DECIMAL(3,2) DEFAULT 0.0,
    review_count INTEGER DEFAULT 0,
    is_featured BOOLEAN DEFAULT false,
    is_active BOOLEAN DEFAULT true,
    seller_id UUID REFERENCES public.users(id),
    created_by UUID REFERENCES public.users(id),
    updated_by UUID REFERENCES public.users(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 购物车表
CREATE TABLE IF NOT EXISTS public.cart_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    product_id UUID REFERENCES public.products(id) ON DELETE CASCADE,
    quantity INTEGER DEFAULT 1,
    added_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, product_id)
);

-- 订单表
CREATE TABLE IF NOT EXISTS public.orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    order_number VARCHAR(50) UNIQUE NOT NULL,
    total_amount DECIMAL(10,2) NOT NULL,
    currency VARCHAR(10) DEFAULT 'USD',
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'processing', 'shipped', 'delivered', 'cancelled', 'refunded')),
    payment_status VARCHAR(20) DEFAULT 'pending' CHECK (payment_status IN ('pending', 'paid', 'failed', 'refunded')),
    payment_method VARCHAR(50),
    shipping_address JSONB,
    tracking_number VARCHAR(100),
    notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 订单明细表
CREATE TABLE IF NOT EXISTS public.order_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID REFERENCES public.orders(id) ON DELETE CASCADE,
    product_id UUID REFERENCES public.products(id),
    product_name VARCHAR(200) NOT NULL,
    product_price DECIMAL(10,2) NOT NULL,
    quantity INTEGER NOT NULL,
    subtotal DECIMAL(10,2) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 电商服务索引
CREATE INDEX IF NOT EXISTS idx_products_category ON public.products(category);
CREATE INDEX IF NOT EXISTS idx_products_seller_id ON public.products(seller_id);
CREATE INDEX IF NOT EXISTS idx_products_is_featured ON public.products(is_featured);
CREATE INDEX IF NOT EXISTS idx_products_is_active ON public.products(is_active);
CREATE INDEX IF NOT EXISTS idx_cart_items_user_id ON public.cart_items(user_id);
CREATE INDEX IF NOT EXISTS idx_orders_user_id ON public.orders(user_id);
CREATE INDEX IF NOT EXISTS idx_orders_status ON public.orders(status);
CREATE INDEX IF NOT EXISTS idx_orders_order_number ON public.orders(order_number);
CREATE INDEX IF NOT EXISTS idx_order_items_order_id ON public.order_items(order_id);

-- ============================================
-- 通用表
-- ============================================

-- 评论表 (通用)
CREATE TABLE IF NOT EXISTS public.reviews (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    target_type VARCHAR(50) NOT NULL CHECK (target_type IN ('city', 'coworking', 'hotel', 'event', 'product')),
    target_id UUID NOT NULL,
    rating DECIMAL(3,2) NOT NULL CHECK (rating >= 0 AND rating <= 5),
    content TEXT,
    images TEXT[],
    helpful_count INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 收藏表 (通用)
CREATE TABLE IF NOT EXISTS public.favorites (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    target_type VARCHAR(50) NOT NULL CHECK (target_type IN ('city', 'coworking', 'hotel', 'event', 'innovation', 'product')),
    target_id UUID NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, target_type, target_id)
);

-- 聊天消息表
CREATE TABLE IF NOT EXISTS public.chat_messages (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    room_id VARCHAR(100) NOT NULL,
    sender_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    message TEXT NOT NULL,
    message_type VARCHAR(20) DEFAULT 'text' CHECK (message_type IN ('text', 'image', 'file', 'system')),
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 通知表
CREATE TABLE IF NOT EXISTS public.notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES public.users(id) ON DELETE CASCADE,
    type VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    content TEXT,
    data JSONB,
    is_read BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 通用表索引
CREATE INDEX IF NOT EXISTS idx_reviews_target ON public.reviews(target_type, target_id);
CREATE INDEX IF NOT EXISTS idx_reviews_user_id ON public.reviews(user_id);
CREATE INDEX IF NOT EXISTS idx_favorites_user_id ON public.favorites(user_id);
CREATE INDEX IF NOT EXISTS idx_favorites_target ON public.favorites(target_type, target_id);
CREATE INDEX IF NOT EXISTS idx_chat_messages_room_id ON public.chat_messages(room_id);
CREATE INDEX IF NOT EXISTS idx_chat_messages_sender_id ON public.chat_messages(sender_id);
CREATE INDEX IF NOT EXISTS idx_notifications_user_id ON public.notifications(user_id);
CREATE INDEX IF NOT EXISTS idx_notifications_is_read ON public.notifications(is_read);

-- ============================================
-- 触发器 - 自动更新 updated_at 字段
-- ============================================

CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 为所有需要的表创建触发器
DO $$
DECLARE
    t text;
BEGIN
    FOR t IN 
        SELECT table_name 
        FROM information_schema.columns 
        WHERE column_name = 'updated_at' 
        AND table_schema = 'public'
    LOOP
        EXECUTE format('DROP TRIGGER IF EXISTS update_%I_updated_at ON public.%I', t, t);
        EXECUTE format('CREATE TRIGGER update_%I_updated_at 
                       BEFORE UPDATE ON public.%I 
                       FOR EACH ROW 
                       EXECUTE FUNCTION update_updated_at_column()', t, t);
    END LOOP;
END;
$$ language 'plpgsql';

-- ============================================
-- 行级安全策略 (Row Level Security)
-- ============================================

-- 启用 RLS
ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.cities ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.coworking_spaces ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.hotels ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.events ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.innovations ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.travel_plans ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.products ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.reviews ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.favorites ENABLE ROW LEVEL SECURITY;

-- 用户表策略
CREATE POLICY "Users can view all profiles" ON public.users FOR SELECT USING (true);
CREATE POLICY "Users can update own profile" ON public.users FOR UPDATE USING (auth.uid()::text = id::text);

-- 城市表策略 (公开读取,管理员写入)
CREATE POLICY "Cities are viewable by everyone" ON public.cities FOR SELECT USING (is_active = true);
CREATE POLICY "Admins can manage cities" ON public.cities FOR ALL USING (true);

-- 其他表的策略 (根据需要调整)
CREATE POLICY "Public read access" ON public.coworking_spaces FOR SELECT USING (is_active = true);
CREATE POLICY "Public read access" ON public.hotels FOR SELECT USING (is_active = true);
CREATE POLICY "Public read access" ON public.events FOR SELECT USING (true);
CREATE POLICY "Public read access" ON public.innovations FOR SELECT USING (is_public = true);
CREATE POLICY "Public read access" ON public.products FOR SELECT USING (is_active = true);

-- 用户可以管理自己的数据
CREATE POLICY "Users can manage own travel plans" ON public.travel_plans 
    FOR ALL USING (auth.uid()::text = user_id::text);

CREATE POLICY "Users can manage own favorites" ON public.favorites 
    FOR ALL USING (auth.uid()::text = user_id::text);

CREATE POLICY "Users can manage own reviews" ON public.reviews 
    FOR ALL USING (auth.uid()::text = user_id::text);

-- ============================================
-- 初始化示例数据
-- ============================================

-- 插入示例城市
INSERT INTO public.cities (name, country, region, description, latitude, longitude, climate, timezone, currency, cost_of_living, overall_score, internet_quality_score, safety_score, cost_score, community_score, weather_score, tags) 
VALUES 
    ('Chiang Mai', 'Thailand', 'Northern Thailand', 'A digital nomad paradise with affordable living, great food, and co-working spaces.', 18.7883, 98.9853, 'Tropical', 'UTC+7', 'THB', 800, 9.2, 8.5, 9.0, 9.5, 9.8, 8.0, ARRAY['digital-nomad', 'affordable', 'coworking', 'tropical']),
    ('Lisbon', 'Portugal', 'Lisbon District', 'Vibrant European city with a growing tech scene and beautiful coastline.', 38.7223, -9.1393, 'Mediterranean', 'UTC+0', 'EUR', 1500, 8.8, 9.0, 8.5, 7.5, 9.0, 9.2, ARRAY['europe', 'tech-hub', 'beach', 'culture']),
    ('Bali', 'Indonesia', 'Bali', 'Island paradise with stunning beaches, rice terraces, and a thriving digital nomad community.', -8.3405, 115.0920, 'Tropical', 'UTC+8', 'IDR', 900, 9.0, 8.0, 8.5, 9.0, 9.5, 8.5, ARRAY['beach', 'tropical', 'wellness', 'affordable']),
    ('Barcelona', 'Spain', 'Catalonia', 'Coastal city known for its art, architecture, and Mediterranean lifestyle.', 41.3851, 2.1734, 'Mediterranean', 'UTC+1', 'EUR', 1800, 8.5, 8.8, 8.0, 7.0, 8.5, 9.0, ARRAY['europe', 'beach', 'culture', 'architecture']),
    ('Mexico City', 'Mexico', 'Central Mexico', 'Vibrant capital with rich culture, incredible food, and affordable living.', 19.4326, -99.1332, 'Subtropical Highland', 'UTC-6', 'MXN', 1000, 8.3, 8.0, 7.5, 8.5, 8.5, 8.0, ARRAY['culture', 'food', 'affordable', 'historic'])
ON CONFLICT DO NOTHING;

-- 更新城市的 PostGIS location 字段
UPDATE public.cities SET location = ST_SetSRID(ST_MakePoint(longitude, latitude), 4326)::geography WHERE location IS NULL;

COMMENT ON DATABASE postgres IS 'Go Nomads Digital Nomad Platform Database';
