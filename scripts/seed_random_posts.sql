-- PostgreSQL seed: 20 active sell posts and 20 active buy posts.
-- Run once per desired batch. Requires active personal/business users and active product types.
BEGIN;

DO $seed$
DECLARE
    sell_owners uuid[];
    buy_owners uuid[];
    type_ids uuid[];
    type_category_ids uuid[];
    type_names text[];
    n integer;
    kind integer;
    owner_id uuid;
    type_index integer;
    post_id uuid;
    price numeric(18,2);
    quantity integer;
    created_at timestamptz;
BEGIN
    SELECT array_agg("UserId") INTO sell_owners
    FROM public."Users" WHERE "Role" = 1 AND "Status" = 1;
    SELECT array_agg("UserId") INTO buy_owners
    FROM public."Users" WHERE "Role" = 2 AND "Status" = 1;
    SELECT array_agg(t."ProductTypeId"), array_agg(t."CategoryId"),
           array_agg(coalesce(t."ProductTypeName", 'Đồ gia dụng'))
    INTO type_ids, type_category_ids, type_names
    FROM public."Product_Type" t
    JOIN public."Category" c ON c."CategoryId" = t."CategoryId"
    WHERE t."IsActive" AND c."IsActive";

    IF coalesce(array_length(sell_owners, 1), 0) = 0
       OR coalesce(array_length(buy_owners, 1), 0) = 0
       OR coalesce(array_length(type_ids, 1), 0) = 0 THEN
        RAISE EXCEPTION 'Need active Personal and Business users and active Product_Type/Category rows';
    END IF;

    FOR n IN 1..40 LOOP
        kind := CASE WHEN n <= 20 THEN 1 ELSE 2 END;
        owner_id := CASE WHEN kind = 1
            THEN sell_owners[1 + floor(random() * array_length(sell_owners, 1))::integer]
            ELSE buy_owners[1 + floor(random() * array_length(buy_owners, 1))::integer] END;
        type_index := 1 + floor(random() * array_length(type_ids, 1))::integer;
        post_id := gen_random_uuid();
        quantity := 1 + floor(random() * 5)::integer;
        price := (5 + floor(random() * 196)::integer) * 100000;
        created_at := now() - (floor(random() * 14)::integer || ' days')::interval;

        INSERT INTO public."Post"
            ("PostId", "OwnerId", "Description", "Quantity", "RemainingQuantity",
             "PostType", "BasePrice", "StreetAddress", "Ward", "City",
             "DeliveryMethod", "PriorityLevel", "Status", "IsBusinessPosting",
             "CreatedAt", "UpdatedAt", "ExpiryDate")
        VALUES
            (post_id, owner_id,
             CASE WHEN kind = 1 THEN 'Bán ' ELSE 'Cần mua ' END
                 || type_names[type_index] || ' - dữ liệu mẫu #' || n,
             quantity, quantity, kind, price, 'Địa chỉ dữ liệu mẫu',
             'Phường mẫu', 'Hồ Chí Minh', 1,
             CASE WHEN kind = 2 THEN 1 ELSE NULL END,
             1, kind = 2, created_at, created_at,
             CASE WHEN kind = 2 THEN now() + interval '30 days'
                  ELSE now() + interval '12 months' END);

        INSERT INTO public."Product"
            ("ProductId", "PostId", "CategoryId", "ProductTypeId", "BrandId",
             "ProductName", "OriginalPrice", "DetailDescription")
        VALUES
            (gen_random_uuid(), post_id, type_category_ids[type_index],
             type_ids[type_index], NULL,
             type_names[type_index] || ' mẫu #' || n,
             CASE WHEN kind = 1 THEN price * 1.4 ELSE NULL END,
             CASE WHEN kind = 1 THEN 'Sản phẩm dùng để kiểm thử bài đăng bán.'
                  ELSE 'Yêu cầu thu mua dùng để kiểm thử bài đăng mua.' END);

        -- Current DbContext maps this column; older migrations may not have it yet.
        IF kind = 2 AND EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'Post'
              AND column_name = 'MinExpectedPrice') THEN
            EXECUTE 'UPDATE public."Post" SET "MinExpectedPrice" = $1 WHERE "PostId" = $2'
                USING round(price * 0.6, 2), post_id;
        END IF;
    END LOOP;
END
$seed$;

COMMIT;
